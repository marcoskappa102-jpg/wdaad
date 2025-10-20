Objetivo: Criar um MMORPG completo com Unity Client e .NET Core Server dedicado. O servidor será responsável pela lógica do jogo, NPCs, movimentação dos players, persistência e comunicação com o banco de dados, enquanto o cliente Unity será responsável pelo input, interface, renderização e interação visual. O projeto precisa vir pronto para rodar localmente e com arquitetura escalável.

Instruções detalhadas

Servidor dedicado (.NET Core) Criar projeto .NET Core (console ou web, usando WebSocket) chamado MMOServer.

Servidor deve gerenciar:

Login de usuários com nome de usuário e senha.

Criação e seleção de personagens, com atributos:

nome (string)

raca (string)

classe (string) // usar “classe” e não “class”

Cada raca nasce em um ponto específico no mapa.

Manter estado do mundo: posição de cada player, atualizações de movimento, spawn inicial.

Comunicar-se com banco de dados MySQL ou PostgreSQL para salvar contas e personagens.

O servidor deve:

Aceitar múltiplos clientes simultaneamente (Thread-safe / async)

Enviar mensagens de sincronização de posição para todos os players conectados.

Estrutura de pastas recomendada:

MMOServer/ Program.cs Server/ LoginManager.cs PlayerManager.cs CharacterManager.cs WorldManager.cs DatabaseHandler.cs Models/ Player.cs Character.cs Position.cs Utils/ MessageHandler.cs Serialization.cs

Cliente Unity Projeto Unity 2021.3 LTS ou superior.

Cena inicial: Login

Input para usuário e senha.

Botão de login que envia dados para o servidor via WebSocket ou TCP.

Cena de Criação/Seleção de Personagem

Escolha de nome, raca e classe.

Após criação, enviar dados para o servidor e criar personagem no banco.

Seleção de personagem já existente também envia comando para servidor.

Cena do mundo aberto

Câmera que segue o player:

Zoom com scroll do mouse.

Rotação ao segurar botão direito do mouse e mover.

Movimentação:

Player anda clicando com botão esquerdo no mapa (point-and-click movement).

Receber do servidor a posição de outros players e mostrar eles andando.

Estrutura de pastas recomendada:

UnityClient/ Assets/ Scenes/ Login.unity CharacterSelect.unity World.unity Scripts/ Network/ ClientManager.cs MessageHandler.cs Player/ PlayerController.cs Character.cs UI/ LoginUI.cs CharacterSelectUI.cs Camera/ CameraController.cs

Comunicação Cliente ↔ Servidor Usar WebSocket ou TCP.

Mensagens devem ser serializadas em JSON:

LoginRequest, LoginResponse

CreateCharacterRequest, CreateCharacterResponse

SelectCharacterRequest, SelectCharacterResponse

PlayerPositionUpdate

Servidor envia:

Atualização das posições de todos os players conectados.

Confirmação de login e criação de personagem.

Cliente recebe:

Posicionamento dos outros players.

Confirmações do servidor.

Gameplay básica Ao entrar no mundo:

O player nasce em um ponto específico de acordo com sua raca.

Player pode andar pelo mapa usando clique esquerdo.

Câmera segue o player, permite zoom e rotação.

Outros players conectados aparecem e se movem corretamente no mundo.

Servidor mantém:

Todas as posições em tempo real.

Atualizações enviadas periodicamente para todos os clientes.

Arquivos essenciais para a IA gerar Servidor (.NET Core):

Program.cs → inicializa servidor WebSocket/TCP

LoginManager.cs, CharacterManager.cs, PlayerManager.cs, WorldManager.cs

DatabaseHandler.cs → conexão MySQL/PostgreSQL

Models/Player.cs, Models/Character.cs, Models/Position.cs

Utils/MessageHandler.cs → serialização JSON, tratamento de mensagens

Cliente (Unity):

LoginUI.cs, CharacterSelectUI.cs

ClientManager.cs, MessageHandler.cs

PlayerController.cs, Character.cs

CameraController.cs

Scenes/Login.unity, Scenes/CharacterSelect.unity, Scenes/World.unity

Comportamentos esperados Login envia request → servidor valida → retorna sucesso/falha.

Criação de personagem envia request → servidor salva no banco → retorna personagem criado.

Seleção de personagem → carrega posição inicial.

Spawn do player no mapa → câmera segue → player pode andar com clique do mouse.

Outros players aparecem e se movimentam conforme servidor atualiza posições.

Zoom e rotação funcionam conforme scroll e botão direito.

Extras para IA considerar Nomear variáveis com cuidado (classe e não class).

Fazer código modular e escalável.

Preparar servidor para futura expansão: NPCs, drops, combate, quests.

Usar padrões de projeto onde fizer sentido (Singleton para NetworkManager, Factory para personagens, etc.).

✅ Prompt final resumido para IA

"Crie um MMORPG completo com cliente Unity e servidor dedicado .NET Core. O servidor gerencia login, criação e seleção de personagens (nome, raca, classe), spawn inicial por raça, movimentação de players e sincronização de posições via WebSocket/TCP. O cliente Unity tem cenas de login, criação/seleção de personagem e mundo aberto, com câmera que segue o player, zoom com scroll e rotação com botão direito. Movimentação point-and-click no mapa. Outros players aparecem e se movem conforme servidor envia posições. Use MySQL/PostgreSQL para persistência. Organize arquivos e scripts conforme padrões recomendados, modular e escalável. Forneça todo o código, arquivos e pastas necessários para rodar o servidor e o cliente localmente."
