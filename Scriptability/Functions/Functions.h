// Types
typedef void(__cdecl * Com_Error_t)(errorParm_t code, const char *fmt, ...);

typedef XAssetHeader(__cdecl * DB_FindXAssetHeader_t)(XAssetType type, const char *name, bool errorIfMissing, int waitTime);

typedef int(__cdecl * FS_FOpenFileByMode_t)(const char *qpath, int *f, fsMode_t mode);
typedef void(__cdecl * FS_FCloseFile_t)(int h);
typedef void(__cdecl * FS_FreeFile_t)(const void *buffer);
typedef int(__cdecl * FS_ReadFile_t)(const char *qpath, void **buffer);
typedef const char **(__cdecl * FS_ListFiles_t)(const char *path, const char *extension, FsListBehavior_e behavior, int *numfiles, int allocTrackType);
typedef void(__cdecl * FS_FreeFileList_t)(const char **list, int allocTrackType);
typedef FILE*(__cdecl * FS_FileForHandle_t)(int f);
typedef int(__cdecl * FS_GetFileOsPath_t)(const char *filename, char *ospath);

typedef unsigned short(__cdecl * Scr_ExecThread_t)(scriptInstance_t inst, int handle, unsigned int paramcount);
typedef void(__cdecl * Scr_FreeThread_t)(scriptInstance_t inst, unsigned __int16 handle);
typedef unsigned int(__cdecl * Scr_LoadScriptInternal_t)(scriptInstance_t inst, const char *filename);
typedef int(__cdecl * Scr_GetFunctionHandle_t)(scriptInstance_t inst, const char *filename, const char *name, unsigned int *checksum, bool errorIfMissing);
typedef int(__cdecl * GScr_LoadScriptAndLabel_t)(scriptInstance_t inst, const char *filename, const char *label, int bEnforceExists, bool errorIfMissing);

typedef void(__cdecl * G_ShutdownGame_t)(int freeScripts);

class Functions
{
public:

	// Functions
	static Com_Error_t Com_Error;

	static DB_FindXAssetHeader_t DB_FindXAssetHeader;

	static FS_FreeFile_t FS_FreeFile;
	static FS_ReadFile_t FS_ReadFile;
	static FS_FCloseFile_t FS_FCloseFile;
	static FS_FOpenFileByMode_t FS_FOpenFileByMode;
	static FS_ListFiles_t FS_ListFiles;
	static FS_FreeFileList_t FS_FreeFileList;
	static FS_FileForHandle_t FS_FileForHandle;
	static FS_GetFileOsPath_t FS_GetFileOsPath;

	static Scr_ExecThread_t Scr_ExecThread;
	static Scr_FreeThread_t Scr_FreeThread;
	static Scr_LoadScriptInternal_t Scr_LoadScriptInternal;
	static GScr_LoadScriptAndLabel_t GScr_LoadScriptAndLabel;

	static G_ShutdownGame_t G_ShutdownGame;

	static void Assign();
	static void AssignFromPattern();

	static bool FS_FileExists(const char* file);
};
