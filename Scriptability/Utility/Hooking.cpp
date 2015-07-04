/*
	This project is released under the GPL 2.0 license.
	Some parts are based on research by Bas Timmer and the OpenSteamworks project.
	Please do no evil.

	Initial author: (https://github.com/)Convery
	Started: 2014-11-24
	Notes:
		These are based on an unknown authors work, can't remember who.
*/

#include "..\STDInclude.h"

void Hook::Call::Initialize(uint32_t Place, void *HookToInstall)
{
	_Place = (uint8_t *)Place;
	memcpy(_OriginalCode, _Place, sizeof(_OriginalCode));

	_Original = _Place + sizeof(_OriginalCode) + *(ptrdiff_t *)(_OriginalCode + 1);
	_Hook = HookToInstall;
}
void Hook::Call::InstallHook(void *HookToInstall)
{
	if(HookToInstall)
		_Hook = HookToInstall;

	if(_Hook)
		*(ptrdiff_t *)(_Place + 1) = (uint8_t *)_Hook - _Place - 5;
}
void Hook::Call::ReleaseHook()
{
	memcpy(_Place + 1, _OriginalCode + 1, sizeof(_OriginalCode) - 1);
}		
void Hook::Call::PermanentHook(uint32_t Place, void *HookToInstall)
{
	*(ptrdiff_t *)((uint8_t *)Place + 1) = (uint8_t *)HookToInstall - (uint8_t *)Place - 5;
}

void Hook::Stomp::Initialize(uint32_t Place, void *HookToInstall, uint8_t ByteCount, bool useJump)
{
	_Place = (uint8_t *)Place;
	_ByteCount = ByteCount < sizeof(_OriginalCode) ? ByteCount : sizeof(_OriginalCode);
	memcpy(_OriginalCode, _Place, sizeof(_OriginalCode));

	_Hook = HookToInstall;
	_Jump = useJump;
}
void Hook::Stomp::InstallHook(void *HookToInstall)
{
	if(HookToInstall)
		_Hook = HookToInstall;

	if(_Hook)
	{
		memset(_Place, 0x90, _ByteCount);
		_Place[0] = _Jump ? (uint8_t)0xE9 : (uint8_t)0xE8;
		*(ptrdiff_t *)(_Place + 1) = (uint8_t *)_Hook - _Place - 5;
	}
}
void Hook::Stomp::ReleaseHook()
{
	memcpy(_Place, _OriginalCode, _ByteCount);
}
void Hook::Stomp::PermanentHook(uint32_t Place, void *HookToInstall)
{
	memset((uint8_t *)Place, 0x90, 5);
	*(uint8_t *)Place = 0xE9;
	*(ptrdiff_t *)((uint8_t *)Place + 1) = (uint8_t *)HookToInstall - (uint8_t *)Place - 5;
}

PIMAGE_NT_HEADERS xImageNTHeader(uint32_t ImgBase)
{
	PIMAGE_DOS_HEADER DosHeader;
	PIMAGE_NT_HEADERS Image;

	DosHeader = (PIMAGE_DOS_HEADER)ImgBase;

	if (DosHeader->e_magic == IMAGE_DOS_SIGNATURE) 
	{
		Image = reinterpret_cast< PIMAGE_NT_HEADERS > (DosHeader->e_lfanew + ImgBase);
		if (Image->Signature == IMAGE_NT_SIGNATURE)
		{
			return (PIMAGE_NT_HEADERS)(DosHeader->e_lfanew + ImgBase);
		}
	}
	return 0;
}
uint32_t Hook::IAT::GetIATAddress(const char* ModuleName, const char* FunctionName, uint32_t ImgBase)
	{
		PIMAGE_NT_HEADERS Image;
		PIMAGE_IMPORT_DESCRIPTOR ImportDesc;
		PIMAGE_THUNK_DATA ThunkData;

		Image = xImageNTHeader(ImgBase);
		if(!Image)
			return 0;

		ImportDesc = (PIMAGE_IMPORT_DESCRIPTOR)((Image->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress) + (uint32_t)ImgBase);
		for(int i = 0; ; i++)
		{
			if(ImportDesc->Name == NULL)
				break;

			if(!_stricmp((char *)((uint32_t)ImportDesc->Name + ImgBase), ModuleName))
			{
				if((ImportDesc->OriginalFirstThunk) != NULL)
				{
					for(int c = 0; ; c++)
					{
						ThunkData = (PIMAGE_THUNK_DATA)((uint32_t)ImportDesc->OriginalFirstThunk + c * sizeof(IMAGE_THUNK_DATA) + ImgBase);

						if(ThunkData->u1.AddressOfData == NULL)
							break;

						if(!_stricmp((char *)(((PIMAGE_IMPORT_BY_NAME)((uint32_t)ThunkData->u1.AddressOfData + ImgBase))->Name), FunctionName))
						{
							return (uint32_t)(((PIMAGE_THUNK_DATA)((uint32_t)ImportDesc->FirstThunk + c * sizeof(IMAGE_THUNK_DATA) + (uint32_t)ImgBase))->u1.Function);
						}
					}
				}
			}
		}

		return 0;
	}
uint32_t Hook::IAT::WriteIATAddress(const char* ModuleName, const char* FunctionName, uint32_t ImgBase, void *NewProc)
{
	PIMAGE_NT_HEADERS Image;
	PIMAGE_IMPORT_DESCRIPTOR ImportDesc;
	PIMAGE_THUNK_DATA ThunkData;
	uint32_t oldValue;

	Image = xImageNTHeader(ImgBase);

	if (!Image) return 0;

	ImportDesc = (PIMAGE_IMPORT_DESCRIPTOR)((Image->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress) + (uint32_t)ImgBase);

	HMODULE origHandle = nullptr;
	LPVOID origProc = NULL;

	if(ModuleName != NULL)
		origHandle = GetModuleHandle(ModuleName);
	if(origHandle != nullptr)
		origProc = GetProcAddress(origHandle, FunctionName);
	else
		return 0;

	for (int i=0; ;i++) 
	{
		if (ImportDesc->Name == NULL) break;

		if (!_stricmp((char*)((uint32_t)ImportDesc->Name + (uint32_t)ImgBase),ModuleName)) 
		{
			if ((ImportDesc->OriginalFirstThunk) != 0) 
			{
				for (int c=0; ; c++) 
				{
					ThunkData = (PIMAGE_THUNK_DATA)((uint32_t)ImportDesc->OriginalFirstThunk + c*sizeof(IMAGE_THUNK_DATA) + (uint32_t)ImgBase);

					if (ThunkData->u1.AddressOfData == NULL) break;

					if (ThunkData->u1.AddressOfData & 0x80000000)
					{
						uint32_t ordinalNumber = ThunkData->u1.AddressOfData & 0xFFFFFFF;

						if (GetProcAddress(origHandle, (LPCSTR)ordinalNumber) == origProc)
						{
							oldValue = (uint32_t)(((PIMAGE_THUNK_DATA)((uint32_t)ImportDesc->FirstThunk + c*sizeof(IMAGE_THUNK_DATA) + (uint32_t)ImgBase))->u1.Function);
							((PIMAGE_THUNK_DATA)((uint32_t)ImportDesc->FirstThunk + c*sizeof(IMAGE_THUNK_DATA) + (uint32_t)ImgBase))->u1.Function = (uint32_t)NewProc;
							return oldValue;
						}
					}
					else
					{
						if (!_stricmp((char*)(((PIMAGE_IMPORT_BY_NAME)((uint32_t)ThunkData->u1.AddressOfData + (uint32_t)ImgBase))->Name),FunctionName)) 
						{
							oldValue = (uint32_t)(((PIMAGE_THUNK_DATA)((uint32_t)ImportDesc->FirstThunk + c*sizeof(IMAGE_THUNK_DATA) + (uint32_t)ImgBase))->u1.Function);
							((PIMAGE_THUNK_DATA)((uint32_t)ImportDesc->FirstThunk + c*sizeof(IMAGE_THUNK_DATA) + (uint32_t)ImgBase))->u1.Function = (uint32_t)NewProc;
							return oldValue;
						}
					}
				}
			}
		}
		ImportDesc++;
	}

	return 0;
}

void Hook::Quick::Patch(void *Address, uint64_t Data, uint32_t ByteCount)
{
	switch(ByteCount)
	{
	case 1:
		*(uint8_t *)Address = (uint8_t)Data;
		break;
	case 2:
		*(uint16_t *)Address = (uint16_t)Data;
		break;

	case 4:
		*(uint32_t *)Address = (uint32_t)Data;
		break;

	case 8:
		*(uint64_t *)Address = (uint64_t)Data;
		break;
	}
};
void Hook::Quick::NOP(void *Address, uint32_t Length)
{
	memset(Address, 0x90, Length);
}
void Hook::Quick::Call(void *Address, uint32_t Data, CallPatch PatchType)
{
	switch(PatchType)
	{
	case CallPatch::PATCH_JUMP:
		*(uint8_t*)Address = (uint8_t)0xE9;
		break;

	case CallPatch::PATCH_CALL:
		*(uint8_t*)Address = (uint8_t)0xE8;
		break;

	default:
		break;
	}

	*(uint32_t *)((uint32_t)Address + 1) = (uint32_t)Data - (uint32_t)Address - 5;
}