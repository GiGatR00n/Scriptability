#include "..\STDInclude.h"

DWORD Addresses::scriptParseTreeLoad1_loc = 0;
DWORD Addresses::scriptParseTreeLoad2_loc = 0;
DWORD Addresses::scriptParseTreeLoad3_loc = 0;
DWORD Addresses::scriptParseTreeLoad4_loc = 0;

DWORD Addresses::loadGameTypeScript_loc = 0;
DWORD Addresses::loadGameType_loc = 0;

void Addresses::Assign()
{
	Addresses::AssignFromPattern();

	switch (Global::Game::Version)
	{
	case GAME_VERSION_39:
	{
		Addresses::loadGameTypeScript_loc = 0;
		Addresses::loadGameType_loc = 0;
		break;
	}

	case GAME_VERSION_40:
	{
		Addresses::loadGameTypeScript_loc = 0x488285;
		Addresses::loadGameType_loc = 0x697E52;
		break;
	}

	case GAME_VERSION_41:
	{
		Addresses::loadGameTypeScript_loc = 0;
		Addresses::loadGameType_loc = 0;
		break;
	}

	case GAME_VERSION_43:
	{
		Addresses::loadGameTypeScript_loc = 0x5B5A05;
		Addresses::loadGameType_loc = 0x4A1CE2;
		break;
	}
	}
}

void Addresses::AssignFromPattern()
{
	Addresses::scriptParseTreeLoad1_loc = (DWORD)Patterns::FindPattern((uint8_t*)"\x6A\x30\xE8\x00\x00\x00\x00\x8B\x40\x08", "xxx????xxx") + 2;
	Addresses::scriptParseTreeLoad2_loc = (DWORD)Patterns::FindPattern((uint8_t*)"\x6A\x30\xE8\x00\x00\x00\x00\x83\xC4\x24", "xxx????xxx") + 2;
	Addresses::scriptParseTreeLoad3_loc = (DWORD)Patterns::FindPattern((uint8_t*)"\x6A\x30\xE8\x00\x00\x00\x00\x8B\x68\x08", "xxx????xxx") + 2;
	Addresses::scriptParseTreeLoad4_loc = (DWORD)Patterns::FindPattern((uint8_t*)"\x6A\x30\xE8\x00\x00\x00\x00\x8B\x70\x08", "xxx????xxx") + 2;
}
