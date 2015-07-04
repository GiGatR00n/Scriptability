#include "..\STDInclude.h"

int Global::Main::PreInit()
{
	// Check environment
	if (!Global::Game::IsValidGame())
	{
		MessageBox(0, "Invalid game environment!", PROJECTLONG, MB_ICONERROR);
		return FALSE;
	}

	/*char* result = Global::Dependency::Loaded();

	if (result)
	{
		MessageBox(0, va("Dependency '%s' not loaded!", result), PROJECTLONG, MB_ICONERROR);
		return FALSE;
	}*/

	if (!Global::Dependency::Import::LoadImports())
	{
		MessageBox(0, "Failed to load imports!", PROJECTLONG, MB_ICONERROR);
		return FALSE;
	}

	// Assign values according to the current gameversion
	Addresses::Assign();
	Functions::Assign();

	// Main initialization
	Script::Apply();

	return TRUE;
}

int Global::Main::PostInit()
{
	return TRUE;
}

int Global::Main::ParseMessage(const char* message)
{
	return TRUE;
}