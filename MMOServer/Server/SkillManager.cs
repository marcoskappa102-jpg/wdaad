using MMOServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MMOServer.Server
{
    /// <summary>
    /// Gerenciador de Skills - Responsável por carregar, castear e gerenciar skills
    /// </summary>
    public class SkillManager
    {
        private static SkillManager? instance;
        public static SkillManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new SkillManager();
                return instance;
            }
        }

        private Dictionary<int, SkillTemplate> skillTemplates = new Dictionary<int, SkillTemplate>();
        private Dictionary<int, Dictionary<int, SkillInstance>> playerSkills = new Dictionary<int, Dictionary<int, SkillInstance>>();
        private Dictionary<int, List<ActiveBuff>> activeBuffs = new Dictionary<int, List<ActiveBuff>>();
        private Random random = new Random();

        private const string CONFIG_FILE = "skills.json";
        private const string CONFIG_FOLDER = "Config";
        private int nextBuffId = 1;

        public void Initialize()
        {
            Console.WriteLine("⚡ SkillManager: Initializing...");
            LoadSkillTemplates();
            Console.WriteLine($"✅ SkillManager: Loaded {skillTemplates.Count} skill templates");
        }

        private void LoadSkillTemplates()
        {
            string filePath = Path.Combine(CONFIG_FOLDER, CONFIG_FILE);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"⚠️ {CONFIG_FILE} not found!");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var config = JsonConvert.DeserializeObject<SkillConfig>(json);

                if (config?.skills != null)
                {
                    foreach (var skill in config.skills)
                    {
                        skillTemplates[skill.id] = skill;
                    }
                    Console.WriteLine($"✅ Loaded {skillTemplates.Count} skills from skills.json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading skills.json: {ex.Message}");
            }
        }

        public SkillTemplate? GetSkillTemplate(int skillId)
        {
            skillTemplates.TryGetValue(skillId, out var template);
            return template;
        }

        public bool LearnSkill(int characterId, int skillId)
        {
            var template = GetSkillTemplate(skillId);
            if (template == null)
            {
                Console.WriteLine($"❌ Skill {skillId} not found");
                return false;
            }

            var character = DatabaseHandler.Instance.GetCharacter(characterId);
            if (character == null)
            {
                Console.WriteLine($"❌ Character {characterId} not found");
                return false;
            }

            if (character.level < template.levelRequired)
            {
                Console.WriteLine($"❌ {character.nome} não tem level suficiente para aprender {template.name}");
                return false;
            }

            if (template.classRequired != 0 && GetClassId(character.classe) != template.classRequired)
            {
                Console.WriteLine($"❌ {template.name} não é compatível com a classe {character.classe}");
                return false;
            }

            if (!playerSkills.ContainsKey(characterId))
            {
                playerSkills[characterId] = new Dictionary<int, SkillInstance>();
            }

            if (playerSkills[characterId].ContainsKey(skillId))
            {
                Console.WriteLine($"⚠️ {character.nome} já aprendeu {template.name}");
                return false;
            }

            var skillInstance = new SkillInstance
            {
                skillId = skillId,
                characterId = characterId,
                level = 1,
                isLearned = true,
                lastCastTime = DateTime.MinValue
            };

            playerSkills[characterId][skillId] = skillInstance;
            DatabaseHandler.Instance.SaveCharacterSkill(skillInstance);
            Console.WriteLine($"✅ {character.nome} aprendeu {template.name}!");
            return true;
        }

        public List<SkillTemplate> GetLearnableSkills(Character character)
        {
            var learnable = new List<SkillTemplate>();
            int classId = GetClassId(character.classe);

            foreach (var skill in skillTemplates.Values)
            {
                if (playerSkills.ContainsKey(character.id) && 
                    playerSkills[character.id].ContainsKey(skill.id))
                {
                    continue;
                }

                if (character.level < skill.levelRequired)
                    continue;

                if (skill.classRequired != 0 && skill.classRequired != classId)
                    continue;

                learnable.Add(skill);
            }

            return learnable;
        }

        public SkillCastResult CastSkill(int characterId, int skillId, int targetId, float currentTime)
        {
            var character = DatabaseHandler.Instance.GetCharacter(characterId);
            if (character == null)
            {
                return CreateFailResult(skillId, "CASTER_NOT_FOUND");
            }

            var template = GetSkillTemplate(skillId);
            if (template == null)
            {
                return CreateFailResult(skillId, "SKILL_NOT_FOUND");
            }

            if (character.isDead)
            {
                return CreateFailResult(skillId, "CASTER_DEAD");
            }

            if (!HasSkillLearned(characterId, skillId))
            {
                return CreateFailResult(skillId, "SKILL_NOT_LEARNED");
            }

            if (character.mana < template.manaCost)
            {
                return CreateFailResult(skillId, "INSUFFICIENT_MANA");
            }

            if (character.health <= template.healthCost)
            {
                return CreateFailResult(skillId, "INSUFFICIENT_HEALTH");
            }

            var skillInstance = playerSkills[characterId][skillId];
            float timeSinceLast = (float)(DateTime.UtcNow - skillInstance.lastCastTime).TotalSeconds;

            if (timeSinceLast < template.cooldown)
            {
                return CreateFailResult(skillId, "ON_COOLDOWN");
            }

            if (template.targetType == "single" || template.targetType == "aoe")
            {
                var target = MonsterManager.Instance.GetMonster(targetId);
                if (target == null || !target.isAlive)
                {
                    return CreateFailResult(skillId, "TARGET_NOT_FOUND");
                }

                float distance = CombatManager.Instance.GetDistance(character.position, target.position);
                if (distance > template.range)
                {
                    return CreateFailResult(skillId, "OUT_OF_RANGE");
                }
            }

            character.mana -= template.manaCost;
            character.health -= template.healthCost;

            if (character.health < 0)
                character.health = 0;

            skillInstance.lastCastTime = DateTime.UtcNow;

            var result = new SkillCastResult
            {
                casterId = character.id,
                casterName = character.nome,
                casterType = "player",
                skillId = skillId,
                skillName = template.name,
                success = true,
                castTime = template.castTime,
                targetResults = new List<SkillTargetResult>()
            };

            List<MonsterInstance> targets = DetermineTargets(character, template, targetId);

            foreach (var target in targets)
            {
                var targetResult = ApplySkillEffect(character, template, target);
                result.targetResults.Add(targetResult);
            }

            DatabaseHandler.Instance.UpdateCharacter(character);
            return result;
        }

        private List<MonsterInstance> DetermineTargets(Character caster, SkillTemplate template, int primaryTargetId)
        {
            var targets = new List<MonsterInstance>();

            if (template.targetType == "single")
            {
                var target = MonsterManager.Instance.GetMonster(primaryTargetId);
                if (target != null && target.isAlive)
                {
                    targets.Add(target);
                }
            }
            else if (template.targetType == "aoe")
            {
                var allMonsters = MonsterManager.Instance.GetAliveMonsters();
                int hitCount = 0;

                foreach (var monster in allMonsters)
                {
                    float distance = CombatManager.Instance.GetDistance(caster.position, monster.position);

                    if (distance <= template.aoeRadius)
                    {
                        targets.Add(monster);
                        hitCount++;

                        if (template.maxTargets > 0 && hitCount >= template.maxTargets)
                        {
                            break;
                        }
                    }
                }
            }

            return targets;
        }

        private SkillTargetResult ApplySkillEffect(Character caster, SkillTemplate template, MonsterInstance target)
        {
            var result = new SkillTargetResult
            {
                targetId = target.id,
                targetName = target.template.name,
                targetType = "monster"
            };

            if (template.skillType == "attack")
            {
                result = ApplyDamageSkill(caster, template, target);
            }
            else if (template.skillType == "heal")
            {
                result = ApplyCureSkill(caster, template, target);
            }
            else if (template.skillType == "buff" || template.skillType == "debuff")
            {
                result = ApplyBuffSkill(caster, template, target);
            }

            return result;
        }

        private SkillTargetResult ApplyDamageSkill(Character caster, SkillTemplate template, MonsterInstance target)
        {
            var result = new SkillTargetResult
            {
                targetId = target.id,
                targetName = target.template.name,
                targetType = "monster"
            };

            if (template.canMiss)
            {
                int casterHit = 175 + caster.dexterity + caster.level;
                int targetFlee = 100 + target.template.level + target.template.defense;
                float hitChance = 0.80f + ((casterHit - targetFlee) / 100f);
                hitChance = Math.Clamp(hitChance, 0.30f, 0.95f);

                if (random.NextDouble() > hitChance)
                {
                    result.isMiss = true;
                    Console.WriteLine($"❌ {caster.nome} MISSED {target.template.name} with {template.name}");
                    return result;
                }
            }

            int baseDamage = template.baseDamage;
            baseDamage += (int)(caster.strength * template.strScale);
            baseDamage += (int)(caster.intelligence * template.intScale);
            baseDamage += (int)(caster.dexterity * template.dexScale);
            baseDamage += (int)(caster.vitality * template.vitScale);

            baseDamage = (int)(baseDamage * (1.0f + (caster.level - 1) * template.levelScale * 0.1f));

            float variance = 0.95f + ((float)random.NextDouble() * 0.10f);
            int damage = (int)(baseDamage * template.damageMultiplier * variance);

            damage = Math.Clamp(damage, template.minDamage, template.maxDamage);

            bool isCrit = false;
            if (template.criticalChance > 0)
            {
                if (random.Next(100) < template.criticalChance)
                {
                    damage = (int)(damage * 1.5f);
                    isCrit = true;
                }
            }

            int guaranteedDamage = (int)(damage * 0.1f);
            int defensibleDamage = damage - guaranteedDamage;
            float defReduction = 1.0f - (target.template.defense / (float)(target.template.defense + 100));
            defReduction = Math.Max(defReduction, 0.1f);

            int finalDamage = guaranteedDamage + (int)(defensibleDamage * defReduction);
            finalDamage = Math.Max(finalDamage, 1);

            int actualDamage = target.TakeDamage(finalDamage);

            result.damage = actualDamage;
            result.isCritical = isCrit;
            result.remainingHealth = target.currentHealth;
            result.died = !target.isAlive;

            Console.WriteLine($"⚡ {caster.nome} cast {template.name} on {target.template.name}: {actualDamage} dmg{(isCrit ? " CRIT!" : "")}");

            if (result.died)
            {
                Console.WriteLine($"💀 {target.template.name} died from {template.name}!");
            }

            try
            {
                DatabaseHandler.Instance.LogSkillCast(caster.id, template.id, target.id, 
                    target.template.name, true, actualDamage, 0, isCrit, false);
            }
            catch { }

            return result;
        }

        private SkillTargetResult ApplyCureSkill(Character caster, SkillTemplate template, MonsterInstance target)
        {
            var result = new SkillTargetResult
            {
                targetId = target.id,
                targetName = target.template.name,
                targetType = "monster",
                isMiss = false
            };

            int healAmount = template.effectValue;
            healAmount += (int)(caster.intelligence * template.intScale);
            healAmount += (int)(caster.vitality * template.vitScale);

            float variance = 0.95f + ((float)random.NextDouble() * 0.10f);
            healAmount = (int)(healAmount * variance);

            int oldHealth = caster.health;
            caster.health = Math.Min(caster.health + healAmount, caster.maxHealth);
            int actualHeal = caster.health - oldHealth;

            result.heal = actualHeal;
            result.remainingHealth = caster.health;

            Console.WriteLine($"💚 {caster.nome} cast {template.name}: healed {actualHeal} HP ({oldHealth} -> {caster.health})");

            try
            {
                DatabaseHandler.Instance.LogSkillCast(caster.id, template.id, 0, "self", true, 0, actualHeal, false, false);
            }
            catch { }

            return result;
        }

        private SkillTargetResult ApplyBuffSkill(Character caster, SkillTemplate template, MonsterInstance target)
        {
            var result = new SkillTargetResult
            {
                targetId = target.id,
                targetName = target.template.name,
                targetType = "monster",
                appliedBuffs = new List<ActiveBuff>()
            };

            var buff = new ActiveBuff
            {
                buffId = nextBuffId++,
                buffName = template.name,
                skillName = template.name,
                skillId = template.id,
                casterId = caster.id,
                buffType = template.skillType,
                effectType = template.effectType,
                statBoost = template.effectValue,
                affectedStat = template.effectTarget,
                remainingDuration = template.effectDuration,
                applicationTime = DateTime.UtcNow,
                isActive = true
            };

            if (!activeBuffs.ContainsKey(target.id))
            {
                activeBuffs[target.id] = new List<ActiveBuff>();
            }

            activeBuffs[target.id].Add(buff);
            result.appliedBuffs.Add(buff);

            Console.WriteLine($"✨ {template.name} applied to {target.template.name} for {template.effectDuration}s");

            try
            {
                DatabaseHandler.Instance.SaveActiveBuff(target.id, buff);
            }
            catch { }

            return result;
        }

        public void UpdateBuffs(float deltaTime)
        {
            var buffIds = activeBuffs.Keys.ToList();

            foreach (var targetId in buffIds)
            {
                var buffs = activeBuffs[targetId];
                var expiredBuffs = new List<int>();

                for (int i = buffs.Count - 1; i >= 0; i--)
                {
                    var buff = buffs[i];
                    buff.remainingDuration -= deltaTime;

                    if (buff.remainingDuration <= 0)
                    {
                        buff.isActive = false;
                        expiredBuffs.Add(i);
                    }
                }

                foreach (var idx in expiredBuffs)
                {
                    buffs.RemoveAt(idx);
                }

                if (buffs.Count == 0)
                {
                    activeBuffs.Remove(targetId);
                }
            }
        }

        public List<ActiveBuff> GetActiveBuffs(int characterId)
        {
            activeBuffs.TryGetValue(characterId, out var buffs);
            return buffs ?? new List<ActiveBuff>();
        }

        private bool HasSkillLearned(int characterId, int skillId)
        {
            return playerSkills.ContainsKey(characterId) && 
                   playerSkills[characterId].ContainsKey(skillId);
        }

        private SkillCastResult CreateFailResult(int skillId, string reason)
        {
            return new SkillCastResult
            {
                skillId = skillId,
                success = false,
                failReason = reason,
                targetResults = new List<SkillTargetResult>()
            };
        }

        private int GetClassId(string className)
        {
            return className switch
            {
                "Guerreiro" => 1,
                "Mago" => 2,
                "Arqueiro" => 3,
                "Clerigo" => 4,
                _ => 0
            };
        }

        public Dictionary<int, SkillInstance> GetPlayerSkills(int characterId)
        {
            if (playerSkills.ContainsKey(characterId))
                return playerSkills[characterId];

            return new Dictionary<int, SkillInstance>();
        }

        public void LoadPlayerSkills(int characterId)
        {
            if (!playerSkills.ContainsKey(characterId))
            {
                var skills = DatabaseHandler.Instance.LoadCharacterSkills(characterId);
                playerSkills[characterId] = new Dictionary<int, SkillInstance>();
                
                foreach (var skill in skills)
                {
                    playerSkills[characterId][skill.skillId] = skill;
                }
            }
        }

        public void ReloadConfigs()
        {
            Console.WriteLine("🔄 Reloading skill configurations...");
            skillTemplates.Clear();
            LoadSkillTemplates();
            Console.WriteLine("✅ Skill configurations reloaded!");
        }
    }
}