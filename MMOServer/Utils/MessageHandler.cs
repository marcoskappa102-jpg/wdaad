using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MMOServer.Server;
using MMOServer.Models;

namespace MMOServer.Utils
{
    public static class MessageHandler
    {
        public static string? HandleMessage(string message, string sessionId)
        {
            try
            {
                var json = JObject.Parse(message);
                var type = json["type"]?.ToString();

                switch (type)
                {
                    case "login":
                        return HandleLogin(json);

                    case "register":
                        return HandleRegister(json);
						
					case "ping":
						return HandlePing(json);

                    case "createCharacter":
                        return HandleCreateCharacter(json);

                    case "selectCharacter":
                        return HandleSelectCharacter(json, sessionId);

                    case "moveRequest":
                        return HandleMoveRequest(json, sessionId);

                    case "attackMonster":
                        return HandleAttackMonster(json, sessionId);

                    case "respawnRequest":
                        return HandleRespawnRequest(json, sessionId);
                    
                    case "addStatusPoint":
                        return HandleAddStatusPoint(json, sessionId);

                    case "getInventory":
                        return HandleGetInventory(json, sessionId);

                    case "useItem":
                        return HandleUseItem(json, sessionId);

                    case "equipItem":
                        return HandleEquipItem(json, sessionId);

                    case "unequipItem":
                        return HandleUnequipItem(json, sessionId);

                    case "dropItem":
                        return HandleDropItem(json, sessionId);

                    case "getPlayers":
                        return HandleGetPlayers();

                    case "getMonsters":
                        return HandleGetMonsters();
						
						case "castSkill":
    return HandleCastSkill(json, sessionId);

case "learnSkill":
    return HandleLearnSkill(json, sessionId);

case "getSkills":
    return HandleGetSkills(json, sessionId);

case "getLearnableSkills":
    return HandleGetLearnableSkills(json, sessionId);
	

                    default:
                        return JsonConvert.SerializeObject(new { type = "error", message = "Unknown message type" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error handling message: {ex.Message}");
                return JsonConvert.SerializeObject(new { type = "error", message = ex.Message });
            }
        }

        private static string HandleLogin(JObject json)
        {
            var username = json["username"]?.ToString();
            var password = json["password"]?.ToString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return JsonConvert.SerializeObject(new { type = "loginResponse", success = false, message = "Invalid credentials" });
            }

            var response = LoginManager.Instance.Login(username, password);
            return JsonConvert.SerializeObject(new { type = "loginResponse", data = response });
        }

        private static string HandleRegister(JObject json)
        {
            var username = json["username"]?.ToString();
            var password = json["password"]?.ToString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return JsonConvert.SerializeObject(new { type = "registerResponse", success = false, message = "Invalid data" });
            }

            var success = LoginManager.Instance.Register(username, password);
            return JsonConvert.SerializeObject(new 
            { 
                type = "registerResponse", 
                success = success, 
                message = success ? "Account created successfully" : "Username already exists" 
            });
        }

    private static string HandleCreateCharacter(JObject json)
{
    var accountId = json["accountId"]?.ToObject<int>() ?? 0;
    var nome = json["nome"]?.ToString();
    var raca = json["raca"]?.ToString();
    var classe = json["classe"]?.ToString();

    Console.WriteLine($"📝 Create Character Request:");
    Console.WriteLine($"   Account ID: {accountId}");
    Console.WriteLine($"   Name: {nome}");
    Console.WriteLine($"   Race: {raca}");
    Console.WriteLine($"   Class: {classe}");

    // ✅ VALIDAÇÃO BÁSICA
    if (accountId == 0)
    {
        Console.WriteLine("❌ Invalid account ID");
        return JsonConvert.SerializeObject(new 
        { 
            type = "createCharacterResponse", 
            success = false, 
            message = "ID de conta inválido" 
        });
    }

    if (string.IsNullOrWhiteSpace(nome) || 
        string.IsNullOrWhiteSpace(raca) || 
        string.IsNullOrWhiteSpace(classe))
    {
        Console.WriteLine("❌ Missing required fields");
        return JsonConvert.SerializeObject(new 
        { 
            type = "createCharacterResponse", 
            success = false, 
            message = "Preencha todos os campos" 
        });
    }

    // ✅ VALIDAÇÃO USANDO CharacterManager
    var validation = CharacterManager.Instance.ValidateCharacterCreation(nome, raca, classe);
    
    if (!validation.valid)
    {
        Console.WriteLine($"❌ Validation failed: {validation.message}");
        return JsonConvert.SerializeObject(new 
        { 
            type = "createCharacterResponse", 
            success = false, 
            message = validation.message 
        });
    }

    // ✅ CRIA O PERSONAGEM (agora usando classes.json)
    var character = CharacterManager.Instance.CreateCharacter(accountId, nome, raca, classe);
    
    if (character != null)
    {
        Console.WriteLine($"✅ Character '{nome}' created successfully!");
        
        return JsonConvert.SerializeObject(new 
        { 
            type = "createCharacterResponse", 
            success = true,
            message = $"Personagem {nome} criado com sucesso!",
            character = new
            {
                id = character.id,
                nome = character.nome,
                raca = character.raca,
                classe = character.classe,
                level = character.level,
                health = character.health,
                maxHealth = character.maxHealth,
                mana = character.mana,
                maxMana = character.maxMana,
                strength = character.strength,
                intelligence = character.intelligence,
                dexterity = character.dexterity,
                vitality = character.vitality,
                attackPower = character.attackPower,
                defense = character.defense,
                position = character.position
            }
        });
    }

    Console.WriteLine("❌ Failed to create character in database");
    return JsonConvert.SerializeObject(new 
    { 
        type = "createCharacterResponse", 
        success = false, 
        message = "Erro ao salvar personagem no banco de dados" 
    });
}

  private static string HandleSelectCharacter(JObject json, string sessionId)
{
    var characterId = json["characterId"]?.ToObject<int>() ?? 0;

    if (characterId == 0)
    {
        return JsonConvert.SerializeObject(new { type = "selectCharacterResponse", success = false, message = "Invalid character" });
    }

    var character = CharacterManager.Instance.GetCharacter(characterId);
    
    if (character != null)
    {
        // ✅ NOVO: Auto-respawn se estiver morto
        if (character.isDead)
        {
            Console.WriteLine($"💀 {character.nome} was dead. Auto-respawning...");
            
            // Pega posição de spawn da raça
            var spawnPosition = CharacterManager.Instance.GetSpawnPosition(character.raca);
            
            // Adiciona pequeno offset aleatório
            Random rand = new Random();
            spawnPosition.x += (float)(rand.NextDouble() * 2 - 1);
            spawnPosition.z += (float)(rand.NextDouble() * 2 - 1);
            
            // Ajusta ao terreno
            TerrainHeightmap.Instance.ClampToGround(spawnPosition, 0f);
            
            // Revive o personagem
            character.Respawn(spawnPosition);
            
            // Salva no banco
            DatabaseHandler.Instance.UpdateCharacter(character);
            
            Console.WriteLine($"✨ {character.nome} auto-respawned at ({spawnPosition.x:F1}, {spawnPosition.y:F1}, {spawnPosition.z:F1})");
        }
        
        var player = new Player
        {
            sessionId = sessionId,
            character = character,
            position = character.position,
            lastAttackTime = -999f
        };

        PlayerManager.Instance.AddPlayer(sessionId, player);

        var allPlayers = PlayerManager.Instance.GetAllPlayers()
            .Select(p => new
            {
                playerId = p.sessionId,
                characterName = p.character.nome,
                position = p.position,
                raca = p.character.raca,
                classe = p.character.classe,
                level = p.character.level,
                health = p.character.health,
                maxHealth = p.character.maxHealth,
                mana = p.character.mana,
                maxMana = p.character.maxMana,
                experience = p.character.experience,
                statusPoints = p.character.statusPoints,
                isMoving = p.isMoving,
                targetPosition = p.targetPosition,
                inCombat = p.inCombat,
                targetMonsterId = p.targetMonsterId,
                isDead = p.character.isDead
            }).ToList();

        var allMonsters = MonsterManager.Instance.GetAllMonsterStates();
        var inventory = ItemManager.Instance.LoadInventory(characterId);

        var newPlayerMessage = JsonConvert.SerializeObject(new
        {
            type = "playerJoined",
            player = new
            {
                playerId = sessionId,
                characterName = character.nome,
                position = character.position,
                raca = character.raca,
                classe = character.classe,
                level = character.level,
                health = character.health,
                maxHealth = character.maxHealth
            }
        });

        Console.WriteLine($"✅ {character.nome} entered the world [HP: {character.health}/{character.maxHealth}] [Dead: {character.isDead}]");

        return "BROADCAST:" + newPlayerMessage + "|||" +
               JsonConvert.SerializeObject(new 
               { 
                   type = "selectCharacterResponse", 
                   success = true, 
                   character = character,
                   playerId = sessionId,
                   allPlayers = allPlayers,
                   allMonsters = allMonsters,
                   inventory = inventory
               });
    }

    return JsonConvert.SerializeObject(new { type = "selectCharacterResponse", success = false, message = "Character not found" });
}

        private static string HandleMoveRequest(JObject json, string sessionId)
        {
            var targetPosition = json["targetPosition"]?.ToObject<Position>();

            if (targetPosition == null)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Invalid target position" });
            }

            var player = PlayerManager.Instance.GetPlayer(sessionId);
            if (player == null)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
            }

            if (player.inCombat)
            {
                player.CancelCombat();
                Console.WriteLine($"🚶 {player.character.nome} stopped attacking (manual move)");
            }

            var success = PlayerManager.Instance.SetPlayerTarget(sessionId, targetPosition);

            if (success)
            {
                return JsonConvert.SerializeObject(new
                {
                    type = "moveAccepted",
                    targetPosition = targetPosition
                });
            }

            return JsonConvert.SerializeObject(new { type = "error", message = "Failed to set target" });
        }

        private static string HandleAttackMonster(JObject json, string sessionId)
        {
            var monsterId = json["monsterId"]?.ToObject<int>() ?? 0;

            if (monsterId == 0)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Invalid monster ID" });
            }

            var player = PlayerManager.Instance.GetPlayer(sessionId);
            var monster = MonsterManager.Instance.GetMonster(monsterId);

            if (player == null)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
            }

            if (player.character.isDead)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Cannot attack while dead" });
            }

            if (monster == null || !monster.isAlive)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Monster not found or dead" });
            }

            if (player.inCombat && player.targetMonsterId == monsterId)
            {
                Console.WriteLine($"⚠️ {player.character.nome} already attacking {monster.template.name}");
                return JsonConvert.SerializeObject(new
                {
                    type = "attackStarted",
                    monsterId = monsterId,
                    monsterName = monster.template.name,
                    alreadyInCombat = true
                });
            }

            if (player.inCombat && player.targetMonsterId != monsterId)
            {
                Console.WriteLine($"🔄 {player.character.nome} switching target to {monster.template.name}");
            }

            player.inCombat = true;
            player.targetMonsterId = monsterId;
            player.targetPosition = new Position
            {
                x = monster.position.x,
                y = monster.position.y,
                z = monster.position.z
            };
            player.isMoving = true;

            Console.WriteLine($"⚔️ {player.character.nome} started attacking {monster.template.name} [ASPD: {player.character.attackSpeed:F2}s]");

            return JsonConvert.SerializeObject(new
            {
                type = "attackStarted",
                monsterId = monsterId,
                monsterName = monster.template.name,
                alreadyInCombat = false
            });
        }

        private static string HandleRespawnRequest(JObject json, string sessionId)
        {
            var player = PlayerManager.Instance.GetPlayer(sessionId);

            if (player == null)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
            }

            if (!player.character.isDead)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Character is not dead" });
            }

            var spawnPosition = CharacterManager.Instance.GetSpawnPosition(player.character.raca);
            
            Random rand = new Random();
            spawnPosition.x += (float)(rand.NextDouble() * 2 - 1);
            spawnPosition.z += (float)(rand.NextDouble() * 2 - 1);
            
            TerrainHeightmap.Instance.ClampToGround(spawnPosition, 0f);
            
            player.character.Respawn(spawnPosition);
            
            player.position.x = spawnPosition.x;
            player.position.y = spawnPosition.y;
            player.position.z = spawnPosition.z;
            
            player.CancelCombat();
            player.lastAttackTime = -999f;

            DatabaseHandler.Instance.UpdateCharacter(player.character);

            Console.WriteLine($"✨ {player.character.nome} ({player.character.raca}) respawned at ({spawnPosition.x:F1}, {spawnPosition.y:F1}, {spawnPosition.z:F1})");

            WorldManager.Instance.BroadcastPlayerRespawn(player);

            return JsonConvert.SerializeObject(new
            {
                type = "respawnResponse",
                success = true,
                position = spawnPosition,
                health = player.character.health,
                maxHealth = player.character.maxHealth
            });
        }

        private static string HandleAddStatusPoint(JObject json, string sessionId)
        {
            var stat = json["stat"]?.ToString();

            if (string.IsNullOrEmpty(stat))
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Invalid stat" });
            }

            var player = PlayerManager.Instance.GetPlayer(sessionId);

            if (player == null)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
            }

            if (player.character.statusPoints <= 0)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "No status points available" });
            }

            bool success = player.character.AddStatusPoint(stat);

            if (success)
            {
                DatabaseHandler.Instance.UpdateCharacter(player.character);

                Console.WriteLine($"📈 {player.character.nome} added point to {stat.ToUpper()}");

                var message = new
                {
                    type = "statusPointAdded",
                    playerId = sessionId,
                    characterName = player.character.nome,
                    stat = stat,
                    statusPoints = player.character.statusPoints,
                    newStats = new
                    {
                        strength = player.character.strength,
                        intelligence = player.character.intelligence,
                        dexterity = player.character.dexterity,
                        vitality = player.character.vitality,
                        maxHealth = player.character.maxHealth,
                        maxMana = player.character.maxMana,
                        attackPower = player.character.attackPower,
                        magicPower = player.character.magicPower,
                        defense = player.character.defense,
                        attackSpeed = player.character.attackSpeed
                    }
                };

                return "BROADCAST:" + JsonConvert.SerializeObject(message);
            }

            return JsonConvert.SerializeObject(new { type = "error", message = "Failed to add status point" });
        }
		
private static string HandleCastSkill(JObject json, string sessionId)
{
    var skillId = json["skillId"]?.ToObject<int>() ?? 0;
    var targetId = json["targetId"]?.ToObject<int>() ?? 0;

    if (skillId == 0)
    {
        return JsonConvert.SerializeObject(new { type = "error", message = "Invalid skill ID" });
    }

    var player = PlayerManager.Instance.GetPlayer(sessionId);
    if (player == null)
    {
        return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
    }

    if (player.character.isDead)
    {
        return JsonConvert.SerializeObject(new 
        { 
            type = "castSkillResponse", 
            success = false, 
            message = "Você está morto!" 
        });
    }

    var currentTime = (float)(DateTime.UtcNow - DateTime.MinValue).TotalSeconds;
    var result = SkillManager.Instance.CastSkill(player.character.id, skillId, targetId, currentTime);

    Console.WriteLine($"⚡ Skill Cast: {player.character.nome} cast {result.skillName}");

    var castMessage = new
    {
        type = "skillCast",
        playerId = sessionId,
        characterName = player.character.nome,
        skillId = result.skillId,
        skillName = result.skillName,
        success = result.success,
        failReason = result.failReason,
        castTime = result.castTime,
        casterStats = new
        {
            health = player.character.health,
            maxHealth = player.character.maxHealth,
            mana = player.character.mana,
            maxMana = player.character.maxMana
        },
        targetResults = result.targetResults.Select(tr => new
        {
            targetId = tr.targetId,
            targetName = tr.targetName,
            targetType = tr.targetType,
            damage = tr.damage,
            heal = tr.heal,
            isCritical = tr.isCritical,
            isMiss = tr.isMiss,
            remainingHealth = tr.remainingHealth,
            remainingMana = tr.remainingMana,
            died = tr.died,
            appliedBuffs = tr.appliedBuffs.Select(b => new
            {
                buffId = b.buffId,
                buffName = b.buffName,
                effectType = b.effectType,
                statBoost = b.statBoost,
                affectedStat = b.affectedStat,
                remainingDuration = b.remainingDuration
            }).ToList()
        }).ToList()
    };

    if (result.success && result.targetResults.Count > 0)
    {
        return "BROADCAST:" + JsonConvert.SerializeObject(castMessage);
    }
    else
    {
        return JsonConvert.SerializeObject(castMessage);
    }
}

private static string HandleLearnSkill(JObject json, string sessionId)
{
    var skillId = json["skillId"]?.ToObject<int>() ?? 0;

    if (skillId == 0)
    {
        return JsonConvert.SerializeObject(new { type = "error", message = "Invalid skill ID" });
    }

    var player = PlayerManager.Instance.GetPlayer(sessionId);
    if (player == null)
    {
        return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
    }

    var skillTemplate = SkillManager.Instance.GetSkillTemplate(skillId);
    if (skillTemplate == null)
    {
        return JsonConvert.SerializeObject(new { type = "error", message = "Skill not found" });
    }

    if (player.character.level < skillTemplate.levelRequired)
    {
        return JsonConvert.SerializeObject(new
        {
            type = "learnSkillResponse",
            success = false,
            message = $"Você precisa estar no level {skillTemplate.levelRequired} para aprender este skill"
        });
    }

    bool learned = SkillManager.Instance.LearnSkill(player.character.id, skillId);

    if (learned)
    {
        var playerSkills = SkillManager.Instance.GetPlayerSkills(player.character.id);

        return JsonConvert.SerializeObject(new
        {
            type = "learnSkillResponse",
            success = true,
            message = $"Você aprendeu {skillTemplate.name}!",
            skill = new
            {
                id = skillTemplate.id,
                name = skillTemplate.name,
                description = skillTemplate.description,
                manaCost = skillTemplate.manaCost,
                cooldown = skillTemplate.cooldown,
                range = skillTemplate.range,
                iconPath = skillTemplate.iconPath
            },
            totalSkills = playerSkills.Count
        });
    }
    else
    {
        return JsonConvert.SerializeObject(new
        {
            type = "learnSkillResponse",
            success = false,
            message = "Você já conhece este skill"
        });
    }
}

private static string HandleGetSkills(JObject json, string sessionId)
{
    var player = PlayerManager.Instance.GetPlayer(sessionId);
    if (player == null)
    {
        return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
    }

    SkillManager.Instance.LoadPlayerSkills(player.character.id);
    var playerSkills = SkillManager.Instance.GetPlayerSkills(player.character.id);
    var skillsData = new List<object>();

    foreach (var kvp in playerSkills)
    {
        var skillInstance = kvp.Value;
        var template = SkillManager.Instance.GetSkillTemplate(skillInstance.skillId);

        if (template == null)
            continue;

        float cooldownRemaining = 0;
        if (skillInstance.lastCastTime != DateTime.MinValue)
        {
            cooldownRemaining = template.cooldown - (float)(DateTime.UtcNow - skillInstance.lastCastTime).TotalSeconds;
            cooldownRemaining = Math.Max(0, cooldownRemaining);
        }

        skillsData.Add(new
        {
            id = template.id,
            name = template.name,
            description = template.description,
            skillType = template.skillType,
            targetType = template.targetType,
            manaCost = template.manaCost,
            cooldown = template.cooldown,
            cooldownRemaining = cooldownRemaining,
            castTime = template.castTime,
            range = template.range,
            aoeRadius = template.aoeRadius,
            level = skillInstance.level,
            iconPath = template.iconPath
        });
    }

    return JsonConvert.SerializeObject(new
    {
        type = "skillsResponse",
        success = true,
        skills = skillsData
    });
}

private static string HandleGetLearnableSkills(JObject json, string sessionId)
{
    var player = PlayerManager.Instance.GetPlayer(sessionId);
    if (player == null)
    {
        return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
    }

    var learnableSkills = SkillManager.Instance.GetLearnableSkills(player.character);
    var skillsData = new List<object>();

    foreach (var skill in learnableSkills)
    {
        skillsData.Add(new
        {
            id = skill.id,
            name = skill.name,
            description = skill.description,
            levelRequired = skill.levelRequired,
            skillPointsRequired = skill.skillPointsRequired,
            manaCost = skill.manaCost,
            cooldown = skill.cooldown,
            range = skill.range,
            skillType = skill.skillType,
            iconPath = skill.iconPath
        });
    }

    return JsonConvert.SerializeObject(new
    {
        type = "learnableSkillsResponse",
        success = true,
        skills = skillsData
    });
}


        // ==================== INVENTÁRIO ====================

        private static string HandleGetInventory(JObject json, string sessionId)
        {
            var player = PlayerManager.Instance.GetPlayer(sessionId);
            
            if (player == null)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
            }

            var inventory = ItemManager.Instance.LoadInventory(player.character.id);

            var itemsData = inventory.items.Select(item => new
            {
                instanceId = item.instanceId,
                templateId = item.templateId,
                quantity = item.quantity,
                slot = item.slot,
                isEquipped = item.isEquipped,
                template = item.template != null ? new
                {
                    id = item.template.id,
                    name = item.template.name,
                    description = item.template.description,
                    type = item.template.type,
                    subType = item.template.subType,
                    slot = item.template.slot,
                    maxStack = item.template.maxStack,
                    iconPath = item.template.iconPath,
                    requiredLevel = item.template.requiredLevel,
                    requiredClass = item.template.requiredClass,
                    effectType = item.template.effectType,
                    effectValue = item.template.effectValue,
                    effectTarget = item.template.effectTarget,
                    bonusStrength = item.template.bonusStrength,
                    bonusIntelligence = item.template.bonusIntelligence,
                    bonusDexterity = item.template.bonusDexterity,
                    bonusVitality = item.template.bonusVitality,
                    bonusMaxHealth = item.template.bonusMaxHealth,
                    bonusMaxMana = item.template.bonusMaxMana,
                    bonusAttackPower = item.template.bonusAttackPower,
                    bonusMagicPower = item.template.bonusMagicPower,
                    bonusDefense = item.template.bonusDefense
                } : null
            }).ToList();

            return JsonConvert.SerializeObject(new
            {
                type = "inventoryResponse",
                success = true,
                inventory = new
                {
                    characterId = inventory.characterId,
                    maxSlots = inventory.maxSlots,
                    gold = inventory.gold,
                    items = itemsData,
                    weaponId = inventory.weaponId,
                    armorId = inventory.armorId,
                    helmetId = inventory.helmetId,
                    bootsId = inventory.bootsId,
                    glovesId = inventory.glovesId,
                    ringId = inventory.ringId,
                    necklaceId = inventory.necklaceId
                }
            });
        }

        private static string HandleUseItem(JObject json, string sessionId)
        {
            var instanceId = json["instanceId"]?.ToObject<int>() ?? 0;

            if (instanceId == 0)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Invalid item" });
            }

            var result = ItemManager.Instance.UseItem(sessionId, instanceId);
            
            var player = PlayerManager.Instance.GetPlayer(sessionId);

            switch (result)
            {
                case "SUCCESS":
                    if (player != null)
                    {
                        var inventory = ItemManager.Instance.LoadInventory(player.character.id);
                        var item = inventory.GetItem(instanceId);
                        
                        var message = new
                        {
                            type = "itemUsed",
                            playerId = sessionId,
                            instanceId = instanceId,
                            health = player.character.health,
                            maxHealth = player.character.maxHealth,
                            mana = player.character.mana,
                            maxMana = player.character.maxMana,
                            remainingQuantity = item?.quantity ?? 0
                        };

                        return "BROADCAST:" + JsonConvert.SerializeObject(message);
                    }
                    break;

                case "HP_FULL":
                    return JsonConvert.SerializeObject(new 
                    { 
                        type = "itemUseFailed", 
                        reason = "HP_FULL",
                        message = "HP já está cheio!" 
                    });

                case "MP_FULL":
                    return JsonConvert.SerializeObject(new 
                    { 
                        type = "itemUseFailed", 
                        reason = "MP_FULL",
                        message = "MP já está cheio!" 
                    });

                case "ON_COOLDOWN":
                    return JsonConvert.SerializeObject(new 
                    { 
                        type = "itemUseFailed", 
                        reason = "ON_COOLDOWN",
                        message = "Aguarde antes de usar outra poção!" 
                    });

                case "PLAYER_DEAD":
                    return JsonConvert.SerializeObject(new 
                    { 
                        type = "error", 
                        message = "Você não pode usar itens enquanto está morto!" 
                    });

                case "NOT_CONSUMABLE":
                    return JsonConvert.SerializeObject(new 
                    { 
                        type = "error", 
                        message = "Este item não pode ser usado!" 
                    });

                default:
                    return JsonConvert.SerializeObject(new 
                    { 
                        type = "error", 
                        message = "Falha ao usar item" 
                    });
            }

            return JsonConvert.SerializeObject(new { type = "error", message = "Failed to use item" });
        }

        private static string HandleEquipItem(JObject json, string sessionId)
        {
            var instanceId = json["instanceId"]?.ToObject<int>() ?? 0;

            if (instanceId == 0)
            {
                return JsonConvert.SerializeObject(new { type = "error", message = "Invalid item" });
            }

            var success = ItemManager.Instance.EquipItem(sessionId, instanceId);

            if (success)
            {
                var player = PlayerManager.Instance.GetPlayer(sessionId);
                
                if (player != null)
                {
                    var inventory = ItemManager.Instance.LoadInventory(player.character.id);
                    
                    var message = new
                    {
                        type = "itemEquipped",
                        playerId = sessionId,
                        instanceId = instanceId,
                        newStats = new
                        {
                            strength = player.character.strength,
                            intelligence = player.character.intelligence,
                            dexterity = player.character.dexterity,
                            vitality = player.character.vitality,
                            maxHealth = player.character.maxHealth,
                            maxMana = player.character.maxMana,
                            attackPower = player.character.attackPower,
                            magicPower = player.character.magicPower,
                            defense = player.character.defense,
                            attackSpeed = player.character.attackSpeed
                        },
                        equipment = new
                        {
                            weaponId = inventory.weaponId,
                            armorId = inventory.armorId,
                            helmetId = inventory.helmetId,
                            bootsId = inventory.bootsId,
                            glovesId = inventory.glovesId,
                            ringId = inventory.ringId,
                            necklaceId = inventory.necklaceId
                        }
                    };

                    return "BROADCAST:" + JsonConvert.SerializeObject(message);
                }
            }

            return JsonConvert.SerializeObject(new { type = "error", message = "Failed to equip item" });
        }

        private static string HandleUnequipItem(JObject json, string sessionId)
        {
            var slot = json["slot"]?.ToString();

            if (string.IsNullOrEmpty(slot))
            {
                Console.WriteLine("❌ HandleUnequipItem: Invalid slot");
                return JsonConvert.SerializeObject(new { type = "error", message = "Invalid slot" });
            }

            Console.WriteLine($"🔧 Unequipping item from slot: {slot} for player: {sessionId}");

            try
            {
                var success = ItemManager.Instance.UnequipItem(sessionId, slot);

                if (success)
                {
                    var player = PlayerManager.Instance.GetPlayer(sessionId);
                    
                    if (player != null)
                    {
                        var inventory = ItemManager.Instance.LoadInventory(player.character.id);
                        
                        var message = new
                        {
                            type = "itemUnequipped",
                            playerId = sessionId,
                            slot = slot,
                            newStats = new
                            {
                                strength = player.character.strength,
                                intelligence = player.character.intelligence,
                                dexterity = player.character.dexterity,
                                vitality = player.character.vitality,
                                maxHealth = player.character.maxHealth,
                                maxMana = player.character.maxMana,
                                attackPower = player.character.attackPower,
                                magicPower = player.character.magicPower,
                                defense = player.character.defense,
                                attackSpeed = player.character.attackSpeed
                            },
                            equipment = new
                            {
                                weaponId = inventory.weaponId,
                                armorId = inventory.armorId,
                                helmetId = inventory.helmetId,
                                bootsId = inventory.bootsId,
                                glovesId = inventory.glovesId,
                                ringId = inventory.ringId,
                                necklaceId = inventory.necklaceId
                            }
                        };

                        Console.WriteLine($"✅ Item unequipped from slot {slot} successfully");
                        return "BROADCAST:" + JsonConvert.SerializeObject(message);
                    }
                    else
                    {
                        Console.WriteLine($"❌ Player not found: {sessionId}");
                        return JsonConvert.SerializeObject(new { type = "error", message = "Player not found" });
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Failed to unequip item from slot {slot}");
                    return JsonConvert.SerializeObject(new { type = "error", message = "Failed to unequip item. Slot may be empty." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in HandleUnequipItem: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                return JsonConvert.SerializeObject(new { type = "error", message = $"Error unequipping item: {ex.Message}" });
            }
        }

        private static string HandleDropItem(JObject json, string sessionId)
        {
            var instanceId = json["instanceId"]?.ToObject<int>() ?? 0;
            var quantity = json["quantity"]?.ToObject<int>() ?? 1;

            if (instanceId == 0)
            {
                Console.WriteLine("❌ HandleDropItem: Invalid instanceId");
                return JsonConvert.SerializeObject(new { type = "error", message = "Item inválido" });
            }

            Console.WriteLine($"📤 HandleDropItem: Player {sessionId} trying to drop item {instanceId} (qty: {quantity})");

            try
            {
                var success = ItemManager.Instance.RemoveItemFromPlayer(sessionId, instanceId, quantity);

                if (success)
                {
                    Console.WriteLine($"✅ Item {instanceId} dropped successfully");
                    
                    var message = new
                    {
                        type = "itemDropped",
                        playerId = sessionId,
                        instanceId = instanceId,
                        quantity = quantity
                    };

                    return "BROADCAST:" + JsonConvert.SerializeObject(message);
                }
                else
                {
                    Console.WriteLine($"❌ Failed to drop item {instanceId}");
                    return JsonConvert.SerializeObject(new { type = "error", message = "Não foi possível dropar o item" });
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"❌ Exception in HandleDropItem: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                return JsonConvert.SerializeObject(new { type = "error", message = $"Erro ao dropar item: {ex.Message}" });
            }
        }

        private static string HandleGetPlayers()
        {
            var players = PlayerManager.Instance.GetAllPlayers()
                .Select(p => new
                {
                    playerId = p.sessionId,
                    characterName = p.character.nome,
                    position = p.position,
                    raca = p.character.raca,
                    classe = p.character.classe,
                    level = p.character.level,
                    health = p.character.health,
                    maxHealth = p.character.maxHealth,
                    isMoving = p.isMoving,
                    inCombat = p.inCombat
                }).ToList();

            return JsonConvert.SerializeObject(new { type = "playersResponse", players = players });
        }

        private static string HandleGetMonsters()
        {
            var monsters = MonsterManager.Instance.GetAllMonsterStates();
            return JsonConvert.SerializeObject(new { type = "monstersResponse", monsters = monsters });
        }
	
		private static string HandlePing(JObject json)
		{
		var timestamp = json["timestamp"]?.ToObject<long>() ?? 0;
    
		// Responde com pong
		return JsonConvert.SerializeObject(new
		{
        type = "pong",
        timestamp = timestamp,
        serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		});
		}
    }
	
}