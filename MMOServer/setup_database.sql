-- ============================================
-- SCRIPT COMPLETO DE BANCO DE DADOS - MMO RPGM
-- ============================================
-- Execute este script COMPLETO no MySQL
-- mysql -u root -p < setup_database_full.sql
-- OU copie e cole no MySQL Workbench

-- ====================
-- PASSO 1: REMOVER BANCO ANTIGO
-- ====================
DROP DATABASE IF EXISTS mmo_game;

-- ====================
-- PASSO 2: CRIAR BANCO NOVO
-- ====================
CREATE DATABASE mmo_game CHARACTER SET utf8mb4 COLLATE=utf8mb4_unicode_ci;
USE mmo_game;

-- ====================
-- TABELA 1: CONTAS
-- ====================
CREATE TABLE accounts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP NULL,
    INDEX idx_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 2: PERSONAGENS
-- ====================
CREATE TABLE characters (
    id INT AUTO_INCREMENT PRIMARY KEY,
    account_id INT NOT NULL,
    nome VARCHAR(50) NOT NULL,
    raca VARCHAR(50) NOT NULL,
    classe VARCHAR(50) NOT NULL,
    
    -- Level e Experiência
    level INT DEFAULT 1,
    experience INT DEFAULT 0,
    status_points INT DEFAULT 0,
    
    -- Vida e Mana
    health INT DEFAULT 100,
    max_health INT DEFAULT 100,
    mana INT DEFAULT 100,
    max_mana INT DEFAULT 100,
    
    -- Atributos Base
    strength INT DEFAULT 10,
    intelligence INT DEFAULT 10,
    dexterity INT DEFAULT 10,
    vitality INT DEFAULT 10,
    
    -- Atributos Calculados
    attack_power INT DEFAULT 10,
    magic_power INT DEFAULT 10,
    defense INT DEFAULT 5,
    attack_speed FLOAT DEFAULT 1.0,
    
    -- Posição no Mundo
    pos_x FLOAT DEFAULT 0,
    pos_y FLOAT DEFAULT 0,
    pos_z FLOAT DEFAULT 0,
    
    -- Estado
    is_dead BOOLEAN DEFAULT FALSE,
    
    -- Timestamps
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP NULL,
    
    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE,
    INDEX idx_account (account_id),
    INDEX idx_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 3: TEMPLATES DE MONSTROS
-- ====================
CREATE TABLE monster_templates (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    level INT NOT NULL,
    max_health INT NOT NULL,
    attack_power INT NOT NULL,
    defense INT NOT NULL,
    experience_reward INT NOT NULL,
    attack_speed FLOAT DEFAULT 1.5,
    movement_speed FLOAT DEFAULT 3.0,
    aggro_range FLOAT DEFAULT 10.0,
    
    -- Spawn Settings
    spawn_x FLOAT NOT NULL,
    spawn_y FLOAT NOT NULL,
    spawn_z FLOAT NOT NULL,
    spawn_radius FLOAT DEFAULT 5.0,
    respawn_time INT DEFAULT 30,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 4: INSTÂNCIAS DE MONSTROS
-- ====================
CREATE TABLE monster_instances (
    id INT AUTO_INCREMENT PRIMARY KEY,
    template_id INT NOT NULL,
    current_health INT NOT NULL,
    pos_x FLOAT NOT NULL,
    pos_y FLOAT NOT NULL,
    pos_z FLOAT NOT NULL,
    is_alive BOOLEAN DEFAULT TRUE,
    last_respawn TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (template_id) REFERENCES monster_templates(id) ON DELETE CASCADE,
    INDEX idx_template (template_id),
    INDEX idx_alive (is_alive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 5: LOG DE COMBATE
-- ====================
CREATE TABLE combat_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    character_id INT NULL,
    monster_id INT NULL,
    damage_dealt INT NOT NULL,
    damage_type VARCHAR(20) DEFAULT 'physical',
    is_critical BOOLEAN DEFAULT FALSE,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE SET NULL,
    INDEX idx_character (character_id),
    INDEX idx_timestamp (timestamp)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 6: INVENTÁRIOS
-- ====================
CREATE TABLE inventories (
    character_id INT PRIMARY KEY,
    max_slots INT DEFAULT 50,
    gold INT DEFAULT 0,
    weapon_id INT NULL,
    armor_id INT NULL,
    helmet_id INT NULL,
    boots_id INT NULL,
    gloves_id INT NULL,
    ring_id INT NULL,
    necklace_id INT NULL,
    FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
    INDEX idx_character (character_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 7: INSTÂNCIAS DE ITENS
-- ====================
CREATE TABLE item_instances (
    instance_id INT PRIMARY KEY,
    character_id INT NOT NULL,
    template_id INT NOT NULL,
    quantity INT DEFAULT 1,
    slot INT DEFAULT -1,
    is_equipped BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
    INDEX idx_character (character_id),
    INDEX idx_template (template_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 8: CONTADOR DE IDS DE ITENS
-- ====================
CREATE TABLE item_id_counter (
    id INT PRIMARY KEY DEFAULT 1,
    next_instance_id INT DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 9: COOLDOWNS DE SKILLS
-- ====================
CREATE TABLE skill_cooldowns (
    id INT AUTO_INCREMENT PRIMARY KEY,
    character_id INT NOT NULL,
    skill_id INT NOT NULL,
    cooldown_until TIMESTAMP NOT NULL,
    
    FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
    UNIQUE KEY unique_cooldown (character_id, skill_id),
    INDEX idx_character (character_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 10: SKILLS DO PERSONAGEM
-- ====================
CREATE TABLE character_skills (
    id INT AUTO_INCREMENT PRIMARY KEY,
    character_id INT NOT NULL,
    skill_id INT NOT NULL,
    skill_level INT DEFAULT 1,
    skill_exp INT DEFAULT 0,
    learned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_cast TIMESTAMP NULL,
    
    FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
    UNIQUE KEY unique_char_skill (character_id, skill_id),
    INDEX idx_character (character_id),
    INDEX idx_skill (skill_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 11: BUFFS ATIVOS
-- ====================
CREATE TABLE active_buffs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    buff_id INT NOT NULL,
    character_id INT NOT NULL,
    skill_id INT NOT NULL,
    caster_id INT NOT NULL,
    buff_type VARCHAR(50) NOT NULL,
    effect_type VARCHAR(50) NOT NULL,
    affected_stat VARCHAR(50) NOT NULL,
    stat_boost INT DEFAULT 0,
    duration_remaining FLOAT DEFAULT 0,
    application_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,
    
    FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
    INDEX idx_character (character_id),
    INDEX idx_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- TABELA 12: LOG DE SKILLS
-- ====================
CREATE TABLE skill_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    character_id INT NOT NULL,
    skill_id INT NOT NULL,
    target_id INT NULL,
    target_type VARCHAR(50) NOT NULL,
    success BOOLEAN DEFAULT TRUE,
    damage_dealt INT DEFAULT 0,
    heal_done INT DEFAULT 0,
    was_critical BOOLEAN DEFAULT FALSE,
    was_miss BOOLEAN DEFAULT FALSE,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
    INDEX idx_character (character_id),
    INDEX idx_skill (skill_id),
    INDEX idx_timestamp (timestamp)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================
-- ÍNDICES DE PERFORMANCE
-- ====================
CREATE INDEX idx_skill_log_char_skill ON skill_log(character_id, skill_id);
CREATE INDEX idx_active_buffs_char ON active_buffs(character_id, is_active);
CREATE INDEX idx_character_skills_char ON character_skills(character_id, skill_id);

-- ====================
-- INSERTAR DADOS DE TESTE
-- ====================

-- Insere conta de teste
INSERT INTO accounts (username, password) VALUES 
('admin', 'admin123'),
('player1', 'password123'),
('player2', 'password456');

-- Insere personagens de teste
INSERT INTO characters (account_id, nome, raca, classe, level, experience, status_points, health, max_health, mana, max_mana, strength, intelligence, dexterity, vitality, attack_power, magic_power, defense, attack_speed, pos_x, pos_y, pos_z, is_dead)
VALUES 
(1, 'Admin', 'Humano', 'Guerreiro', 1, 0, 0, 160, 160, 74, 74, 20, 5, 12, 18, 40, 26, 17, 1.2, 0, 0, 0, FALSE),
(2, 'Mago01', 'Humano', 'Mago', 1, 0, 0, 110, 110, 140, 140, 5, 25, 15, 10, 31, 60, 15, 1.4, 10, 0, 10, FALSE),
(3, 'Arqueiro01', 'Elfo', 'Arqueiro', 1, 0, 0, 130, 130, 100, 100, 12, 5, 25, 13, 50, 30, 16, 1.3, 20, 0, 20, FALSE);

-- Insere templates de monstros
INSERT INTO monster_templates (name, level, max_health, attack_power, defense, experience_reward, attack_speed, movement_speed, aggro_range, spawn_x, spawn_y, spawn_z, spawn_radius, respawn_time) VALUES
('Lobo Selvagem', 1, 50, 8, 2, 15, 1.5, 4.0, 8.0, 10, 0, 10, 8.0, 30),
('Lobo Selvagem', 1, 50, 8, 2, 15, 1.5, 4.0, 8.0, -10, 0, -10, 8.0, 30),
('Goblin Explorador', 2, 80, 12, 3, 25, 1.8, 3.5, 10.0, 15, 0, 0, 10.0, 40),
('Goblin Explorador', 2, 80, 12, 3, 25, 1.8, 3.5, 10.0, 0, 0, 15, 10.0, 40),
('Javali Raivoso', 3, 120, 15, 5, 40, 2.0, 3.0, 7.0, 20, 0, 20, 12.0, 45),
('Aranha Gigante', 2, 70, 10, 2, 20, 1.6, 3.8, 9.0, 50, 0, 40, 10.0, 35),
('Corvo Sombrio', 3, 90, 14, 3, 30, 1.4, 5.0, 12.0, 45, 0, 55, 15.0, 40),
('Lobo das Sombras', 4, 150, 20, 6, 55, 1.7, 4.5, 10.0, 55, 0, 45, 12.0, 50),
('Orc Guerreiro', 5, 200, 25, 8, 70, 2.2, 3.2, 12.0, 70, 0, 70, 15.0, 60),
('Dragão Negro', 10, 1000, 80, 20, 500, 3.0, 2.5, 20.0, 0, 0, 150, 25.0, 300);

-- Insere instâncias de monstros
INSERT INTO monster_instances (template_id, current_health, pos_x, pos_y, pos_z, is_alive)
SELECT id, max_health, spawn_x, spawn_y, spawn_z, TRUE FROM monster_templates;

-- Insere contador de IDs de itens
INSERT INTO item_id_counter (id, next_instance_id) VALUES (1, 1000);

-- Insere inventários para personagens
INSERT INTO inventories (character_id, max_slots, gold)
VALUES 
(1, 50, 1000),
(2, 50, 500),
(3, 50, 500);

-- ====================
-- VERIFICAÇÃO
-- ====================
SELECT '✅ Database setup completed successfully!' AS Status;

-- Mostra tabelas criadas
SHOW TABLES;

-- Conta registros
SELECT 
    'accounts' AS Table_Name, COUNT(*) AS Total_Records FROM accounts
UNION ALL
SELECT 'characters', COUNT(*) FROM characters
UNION ALL
SELECT 'monster_templates', COUNT(*) FROM monster_templates
UNION ALL
SELECT 'monster_instances', COUNT(*) FROM monster_instances
UNION ALL
SELECT 'inventories', COUNT(*) FROM inventories
UNION ALL
SELECT 'character_skills', COUNT(*) FROM character_skills
UNION ALL
SELECT 'active_buffs', COUNT(*) FROM active_buffs
UNION ALL
SELECT 'skill_log', COUNT(*) FROM skill_log;

-- Mostra estrutura de algumas tabelas
DESCRIBE characters;
DESCRIBE character_skills;
DESCRIBE active_buffs;
DESCRIBE skill_log;