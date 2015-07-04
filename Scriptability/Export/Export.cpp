#include "..\STDInclude.h"

#pragma region APIExport

EXPORT uint64_t CONVENTION PreInit()
{
	return Global::Main::PreInit();
}

EXPORT uint64_t CONVENTION PostInit()
{
	return Global::Main::PostInit();
}

EXPORT uint64_t CONVENTION AuthorInfo()
{
	return (uint64_t)"momo5502";
}

EXPORT uint64_t CONVENTION ExtendedInfo()
{
	return (uint64_t)(PROJECTLONG "\n");
}

EXPORT uint64_t CONVENTION OfficialMod()
{
	// Let's consider this an official one ;)
	return TRUE;
}

// Dependencies that need to be loaded.
EXPORT uint64_t CONVENTION DependencyCount()
{
	//return 0; // SteamAPI seems to be bugged
	return Global::Dependency::Count();
}

EXPORT uint64_t CONVENTION GetDependency(uint32_t Index)
{
	return (uint64_t)Global::Dependency::Get(Index);
}

EXPORT uint64_t CONVENTION SendMessageB(const char *Message, void* Callback)
{
	return Global::Main::ParseMessage(Message);
}

#pragma endregion

#pragma region Entrypoint

// DLL entry point
BOOL WINAPI DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
	if (ul_reason_for_call == DLL_PROCESS_ATTACH)
	{
		Global::Game::DetectGame();
	}

	return TRUE;
}

#pragma endregion