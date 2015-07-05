#include "..\STDInclude.h"

namespace Script
{
	struct ScriptParseTreeEntry
	{
		RawFile rawfile;
		std::string name;
		std::string buffer;
	};

	struct ScriptHandle
	{
		std::string name;
		std::string function;
		int handle;
		bool loaded;
	};

	std::vector<ScriptHandle> scriptHandles;
	std::map<std::string, ScriptParseTreeEntry> scripParseTreePool;

	DWORD RunCmdSync(const char* cmd, const char* workingDir, char* cmdline)
	{
		DWORD result = -1;
		STARTUPINFO si;
		PROCESS_INFORMATION pi;

		ZeroMemory(&si, sizeof(si));
		si.cb = sizeof(si);
		ZeroMemory(&pi, sizeof(pi));

		if (CreateProcess(cmd, cmdline, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, workingDir, &si, &pi))
		{
			WaitForSingleObject(pi.hProcess, INFINITE);
			CloseHandle(pi.hProcess);
			CloseHandle(pi.hThread);
			GetExitCodeProcess(pi.hProcess, &result);
		}

		return result;
	}

	// Kinda ugly, but that's the most efficient way for now.
	void CompileGSCFile(const char* rel, const char* file, std::string &buffer)
	{
		buffer.clear();

		std::string compiler = va("%s/Plugins/Scriptability/Compiler.exe", FileIO::GetCurrentDir());
		std::string workingDir = va("%s/Plugins/Scriptability/", FileIO::GetCurrentDir());
		std::string cmdLine = va("compile \"%s\" \"%s\" temp.dat", rel, file);

		RunCmdSync(compiler.data(), workingDir.data(), (char*)cmdLine.data());

		std::basic_string<unsigned char> _buffer;
		if (FileIO::ReadFileIntoBuffer("Plugins/Scriptability/temp.dat", _buffer))
		{
			buffer.append(_buffer.begin(), _buffer.end());
			FileIO::DeleteFile("Plugins/Scriptability/temp.dat");
		}
	}

	XAssetHeader Scr_LoadScriptInternal_Stub(XAssetType type, const char *name, bool errorIfMissing, int waitTime)
	{
		XAssetHeader header;

		if (scripParseTreePool.find(name) != scripParseTreePool.end())
		{
			header.rawfile = &scripParseTreePool[name].rawfile;
		}
		else if (Functions::FS_FileExists(name))
		{
			scripParseTreePool[name] = ScriptParseTreeEntry();
			ScriptParseTreeEntry* entry = &scripParseTreePool[name];

			entry->name = name;
			entry->rawfile.name = entry->name.data();

			entry->rawfile.len = Functions::FS_ReadFile(name, (void**)&entry->rawfile.buffer);

			entry->buffer.clear();
			entry->buffer.append(entry->rawfile.buffer, entry->rawfile.len);

			Functions::FS_FreeFile(entry->rawfile.buffer);

			// Determine if GSC is compiled
			if (*(DWORD*)entry->rawfile.buffer != 0x43534780)
			{
				Global::Dependency::Import::Com_Printf(0, "Script '%s' is not compiled. Compiling...", entry->name.data());

				FILE* fp = 0;
				char file[256] = { 0 };
				Functions::FS_GetFileOsPath(name, file);

				CompileGSCFile(name, file, entry->buffer);

				if (entry->buffer.size() < 4 || *(DWORD*)entry->buffer.data() != 0x43534780)
				{
					std::string message = entry->buffer;
					scripParseTreePool.clear();

					Functions::Com_Error(ERR_SERVERDISCONNECT, "Script compile error!\n\nFailed to compile '%s'\n%s", name, message.data());
				}
			}

			entry->rawfile.buffer = entry->buffer.data();

			header.rawfile = &entry->rawfile;
		}
		else
		{
			header = Functions::DB_FindXAssetHeader(type, name, errorIfMissing, waitTime);
		}

		return header;
	}

	void LoadCustomScripts()
	{
		int amount = 0;
		const char** list = Functions::FS_ListFiles("scripts/", "gsc", FS_LIST_PURE_ONLY, &amount, 0xC);

		Global::Dependency::Import::Com_Printf(0, "Loading scripts (%d):", amount);

		if (scriptHandles.size() > 0) scriptHandles.clear();

		for (int i = 0; i < amount; i++)
		{
			if (strlen(list[i]) < 5 || list[i][strlen(list[i]) - 4] != '.')
			{
				continue;
			}
			else
			{
				char* entryPtr = (char*)list[i];
				entryPtr[strlen(entryPtr) - 4] = 0;
			}

			scriptHandles.push_back(ScriptHandle());
			ScriptHandle* handle = &scriptHandles[scriptHandles.size() - 1];

			handle->name = va("scripts/%s", list[i]);
			handle->function = "init";

			if (Functions::Scr_LoadScriptInternal(SCRIPTINSTANCE_SERVER, handle->name.data()))
			{
				handle->handle = Functions::GScr_LoadScriptAndLabel(SCRIPTINSTANCE_SERVER, handle->name.data(), handle->function.data(), 0, true);

				// Load main function, if init doesn't exist
				if (!handle->handle)
				{
					handle->function = "main";
					handle->handle = Functions::GScr_LoadScriptAndLabel(SCRIPTINSTANCE_SERVER, handle->name.data(), handle->function.data(), 0, true);
				}

				handle->loaded = true;

				Global::Dependency::Import::Com_Printf(0, "Script '%s' loaded", handle->name.data());
			}
			else
			{
				handle->loaded = false;
				Global::Dependency::Import::Com_Printf(0, "Failed to load script '%s'", handle->name.data());
			}
		}

		Functions::FS_FreeFileList(list, 0xC);
	}

	void RunCustomScripts()
	{
		Global::Dependency::Import::Com_Printf(0, "Executing script threads:");

		for (unsigned int i = 0; i < scriptHandles.size(); i++)
		{
			ScriptHandle* scriptHandle = &scriptHandles[i];

			// Skip non-loaded scripts
			if (!scriptHandle->loaded) continue;

			// Execute handle if found
			if (scriptHandle->handle)
			{
				int threadhandle = Functions::Scr_ExecThread(SCRIPTINSTANCE_SERVER, scriptHandle->handle, 0);
				Functions::Scr_FreeThread(SCRIPTINSTANCE_SERVER, threadhandle);
				Global::Dependency::Import::Com_Printf(0, "Executed '%s::%s'", scriptHandle->name.data(), scriptHandle->function.data());
			}
			else
			{
				Global::Dependency::Import::Com_Printf(0, "Failed to execute '%s::%s'", scriptHandle->name.data(), scriptHandle->function.data());
			}
		}
	}

	Hook::Call loadGameTypeScript_hook;
	void __declspec(naked) LoadGameTypeScript_Stub()
	{
		LoadCustomScripts();
		__asm jmp loadGameTypeScript_hook._Original
	}

	Hook::Call loadGameType_hook;
	void __declspec(naked) LoadGameType_Stub()
	{
		RunCustomScripts();
		__asm jmp loadGameType_hook._Original
	}

	Hook::Stomp g_ShutdownGame_hook;
	void G_ShutdownGame_Stub(int freeScripts)
	{
		//if (freeScripts)
		scripParseTreePool.clear();

		g_ShutdownGame_hook.ReleaseHook();
		Functions::G_ShutdownGame(freeScripts);
		g_ShutdownGame_hook.InstallHook();
	}

	void Apply()
	{
		// Allow raw script loading
		QCALL(Addresses::scriptParseTreeLoad1_loc, Scr_LoadScriptInternal_Stub, QPATCH_CALL);
		QCALL(Addresses::scriptParseTreeLoad2_loc, Scr_LoadScriptInternal_Stub, QPATCH_CALL);
		QCALL(Addresses::scriptParseTreeLoad3_loc, Scr_LoadScriptInternal_Stub, QPATCH_CALL);
		QCALL(Addresses::scriptParseTreeLoad4_loc, Scr_LoadScriptInternal_Stub, QPATCH_CALL);

		// No support for zombies right now
		if (Global::Game::Type == GAME_TYPE_ZM) return;

		// Load custom scripts
		loadGameTypeScript_hook.Initialize(Addresses::loadGameTypeScript_loc, LoadGameTypeScript_Stub);
		loadGameTypeScript_hook.InstallHook();

		// Execute custom scripts
		loadGameType_hook.Initialize(Addresses::loadGameType_loc, LoadGameType_Stub);
		loadGameType_hook.InstallHook();

		// Clear scriptpool on game shutdown
		g_ShutdownGame_hook.Initialize((DWORD)Functions::G_ShutdownGame, G_ShutdownGame_Stub);
		g_ShutdownGame_hook.InstallHook();

		// Patch script error handler
		*(BYTE*)Addresses::scriptErrorParam_loc = ERR_SERVERDISCONNECT;
	}
}
