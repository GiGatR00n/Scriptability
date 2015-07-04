#define EXTERN extern "C"
#define DLLEXPORT __declspec(dllexport)
#define EXPORT EXTERN DLLEXPORT
#define CONVENTION __cdecl