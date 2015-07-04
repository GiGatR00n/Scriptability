/*
	This project is released under the GPL 2.0 license.
	Some parts are based on research by Bas Timmer and the OpenSteamworks project.
	Please do no evil.

	Initial author: (https://github.com/)Convery
	Started: 2014-11-24
	Notes:
		These are based on an unknown authors work, can't remember who.
*/

#pragma once

// Sometimes we don't want to deal with an object.
#define QPATCH(a, v, s) Hook::Quick::Patch((void*)(a), (uint64_t)(v), (s))
#define QNOP(a, v) Hook::Quick::NOP((void*)(a), (v))
#define QCALL(a, v, AddCall) Hook::Quick::Call((void*)(a), (uint32_t)(v), (AddCall))
#define QPATCH_CALL Hook::Quick::CallPatch::PATCH_CALL
#define QPATCH_JUMP Hook::Quick::CallPatch::PATCH_JUMP
class Hook

{
public:
	class Call
	{
	public:
		uint8_t _OriginalCode[5];
		uint8_t *_Place;
		void *_Original;
		void *_Hook;

		void Initialize(uint32_t Place, void *HookToInstall = NULL);
		void InstallHook(void *HookToInstall = NULL);
		void ReleaseHook();
		static void PermanentHook(uint32_t Place, void *HookToInstall);
	};
	class Stomp
	{
	public:
		uint8_t _OriginalCode[5];
		uint8_t _ByteCount;
		uint8_t *_Place;
		void *_Hook;
		bool _Jump;

		void Initialize(uint32_t Place, void *HookToInstall = NULL, uint8_t ByteCount = 5, bool useJump = true);
		void InstallHook(void *HookToInstall = NULL);
		void ReleaseHook();
		static void PermanentHook(uint32_t Place, void *HookToInstall);
	};
	class IAT
	{
	public:
		static uint32_t GetIATAddress(const char* ModuleName, const char* FunctionName, uint32_t ImgBase);
		static uint32_t WriteIATAddress(const char* ModuleName, const char* FunctionName, uint32_t ImgBase, void *NewProc);
	};
	class Quick
	{
	public:
		enum class CallPatch
		{
			PATCH_CALL,
			PATCH_JUMP,
			PATCH_NOTHING
		};

		static void Patch(void *Address, uint64_t Data, uint32_t ByteCount);
		static void NOP(void *Address, uint32_t Length);
		static void Call(void *Address, uint32_t Data, CallPatch PatchType);
	};
};