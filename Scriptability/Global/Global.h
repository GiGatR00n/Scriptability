#define PROJECTLONG "Scriptability"

// Game version definition
typedef enum GameVersion
{
	GAME_VERSION_39,
	GAME_VERSION_40,
	GAME_VERSION_41,
	GAME_VERSION_43,
	GAME_VERSION_DEDI_DEBUG,
	//GAME_VERSION_DEDI_SHIP,
	GAME_VERSION_ERROR = -1
};

// Game type definition
typedef enum GameType
{
	GAME_TYPE_MP,
	GAME_TYPE_ZM,
	GAME_TYPE_DEDI,
	GAME_TYPE_ERROR = -1
};

// Global class
class Global
{
	public:

		// Game specific class
		class Game
		{
			public:

				static GameVersion Version;
				static GameType    Type;

				static void DetectGame();
				static bool IsValidGame();
		};

		// Dependency handler
		class Dependency
		{
			public:

				static char* List[];

				static int   Count();
				static char* Get(int index);
				static char* Loaded();

				// TODO: Do that in a more dynamic way
				class Import
				{
					// Imports from ExtendedConsole.Red32n
					public:

						// Types
						typedef void(__cdecl* Com_Printf_t)(int channel, const char* message, ...);
						typedef int(__cdecl* Cmd_ArgC_t)();
						typedef const char*(__cdecl* Cmd_ArgV_t)(int index);
						typedef void(__cdecl* Cmd_AddCommand_t)(const char* name, void(*callback)());
						typedef void(__cdecl* Cmd_ExecuteCommand_t)(const char* command, bool sync);
						// Functions
						static Com_Printf_t         Com_Printf;
						static Cmd_ArgC_t           Cmd_ArgC;
						static Cmd_ArgV_t           Cmd_ArgV;
						static Cmd_AddCommand_t     Cmd_AddCommand;
						static Cmd_ExecuteCommand_t Cmd_ExecuteCommand;

						// Other
						static bool LoadImports();
				};
		};

		class Main
		{
			public:

				static int PreInit();
				static int PostInit();
				static int ParseMessage(const char* message);
		};
};
