#include "..\STDInclude.h"

DB_FindXAssetHeader_t Functions::DB_FindXAssetHeader = 0;

FS_FreeFile_t Functions::FS_FreeFile = 0;
FS_ReadFile_t Functions::FS_ReadFile = 0;
FS_FCloseFile_t Functions::FS_FCloseFile = 0;
FS_FOpenFileByMode_t Functions::FS_FOpenFileByMode = 0;
FS_ListFiles_t Functions::FS_ListFiles = 0;
FS_FreeFileList_t Functions::FS_FreeFileList = 0;
FS_FileForHandle_t Functions::FS_FileForHandle = 0;
FS_GetFileOsPath_t Functions::FS_GetFileOsPath = 0;

Scr_ExecThread_t Functions::Scr_ExecThread = 0;
Scr_FreeThread_t Functions::Scr_FreeThread = 0;
Scr_LoadScriptInternal_t Functions::Scr_LoadScriptInternal = 0;
GScr_LoadScriptAndLabel_t Functions::GScr_LoadScriptAndLabel = 0;

void Functions::Assign()
{
	Functions::AssignFromPattern();

	switch (Global::Game::Version)
	{
	case GAME_VERSION_39:
	{
		Functions::DB_FindXAssetHeader = (DB_FindXAssetHeader_t)0x6653A0;

		Functions::FS_FreeFile = (FS_FreeFile_t)0x40C030;
		Functions::FS_FOpenFileByMode = (FS_FOpenFileByMode_t)0x699780;

		Functions::Scr_ExecThread = (Scr_ExecThread_t)0x0;

		break;
	}

	case GAME_VERSION_40:
	{
		Functions::DB_FindXAssetHeader = (DB_FindXAssetHeader_t)0x526800;

		Functions::FS_FreeFile = (FS_FreeFile_t)0x654390;
		Functions::FS_FOpenFileByMode = (FS_FOpenFileByMode_t)0x588770;
		Functions::FS_FileForHandle = (FS_FileForHandle_t)0x5B2640;

		Functions::Scr_ExecThread = (Scr_ExecThread_t)0x6364E0;
		break;
	}

	case GAME_VERSION_41:
	{
		Functions::DB_FindXAssetHeader = (DB_FindXAssetHeader_t)0x43F460;

		Functions::FS_FreeFile = (FS_FreeFile_t)0x5A2220;
		Functions::FS_FOpenFileByMode = (FS_FOpenFileByMode_t)0x498F50;

		Functions::Scr_ExecThread = (Scr_ExecThread_t)0x0;
		break;
	}

	case GAME_VERSION_43:
	{
		Functions::DB_FindXAssetHeader = (DB_FindXAssetHeader_t)0x6F9030;

		Functions::FS_FreeFile = (FS_FreeFile_t)0x6DC730;
		Functions::FS_FOpenFileByMode = (FS_FOpenFileByMode_t)0x521620;
		Functions::FS_FileForHandle = (FS_FileForHandle_t)0x46D830;

		Functions::Scr_ExecThread = (Scr_ExecThread_t)0x4BB610;
		break;
	}
	}
}

void Functions::AssignFromPattern()
{
	// TODO: Add patterns
	Functions::FS_ReadFile = (FS_ReadFile_t)Patterns::FindPattern((uint8_t*)"\x53\x56\x8B\x74\x24\x0C\x85\xF6", "xxxxxxxx");
	Functions::FS_FreeFileList = (FS_FreeFileList_t)Patterns::FindPattern((uint8_t*)"\x8B\x44\x24\x04\x85\xC0\x74\x0A\x8B\x40\xFC", "xxxxxxxxxxx");
	Functions::FS_ListFiles = (FS_ListFiles_t)Patterns::FindPattern((uint8_t*)"\x8B\x44\x24\x14\x8B\x4C\x24\x10\x8B\x54\x24\x0C\x50\x8B\x44\x24\x0C\x51\x8B\x4C\x24\x0C\x52\x8B\x15\x00\x00\x00\x00", "xxxxxxxxxxxxxxxxxxxxxxxxx????");
	Functions::FS_FCloseFile = (FS_FCloseFile_t)Patterns::FindPattern((uint8_t*)"\xA1\x00\x00\x00\x00\x56\x50\xE8\x00\x00\x00\x00\x8B\x74\x24\x0C\x69\xF6\x00\x00\x00\x00", "x????xxx????xxxxxx????");
	Functions::FS_GetFileOsPath = (FS_GetFileOsPath_t)Patterns::FindPattern((uint8_t*)"\x81\xEC\x00\x00\x00\x00\x56\x8B\xB4\x24\x00\x00\x00\x00\x8D\x44\x24\x04\x68\x00\x00\x00\x00", "xx????xxxx????xxxxx????");

	Functions::Scr_LoadScriptInternal = (Scr_LoadScriptInternal_t)Patterns::FindPattern((uint8_t*)"\x81\xEC\x00\x00\x00\x00\x53\x55\x8B\xAC\x24\x00\x00\x00\x00\x68\x00\x00\x00\x00", "xx????xxxxx????x????");
	Functions::GScr_LoadScriptAndLabel = (GScr_LoadScriptAndLabel_t)Patterns::FindPattern((uint8_t*)"\x8B\x44\x24\x08\x81\xEC\x00\x00\x00\x00\x56\xB9\x00\x00\x00\x00", "xxxxxx????xx????");
	Functions::Scr_FreeThread = (Scr_FreeThread_t)Patterns::FindPattern((uint8_t*)"\x0F\xB7\x44\x24\x00\x89\x44\x24\x08", "xxxx?xxxx");
}

bool Functions::FS_FileExists(const char* file)
{
	return (Functions::FS_FOpenFileByMode(file, 0, FS_READ) > 0);
}
