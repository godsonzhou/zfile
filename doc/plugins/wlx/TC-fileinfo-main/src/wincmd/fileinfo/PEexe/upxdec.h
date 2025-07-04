#ifndef __UPXDEC_H__
#define __UPXDEC_H__

typedef	void * (__stdcall *pfnMemoryAllocator)(DWORD);
typedef	void (__stdcall *pfnPGICallBack)(DWORD, DWORD, LPCTSTR);
typedef struct tagPGIParamsBlock
{
	pfnMemoryAllocator pMemAllocator;	// function pointer to PEX supplied memory allocation
	pfnPGICallBack pCallBack;			// function pointer to PEX callback function
	LPVOID pInBuff;						// pointer to input buffer
	LPVOID pOutBuff;					// pointer to output buffer filled by plugin
	DWORD dwInSize;						// size in bytes of input buffer
	DWORD dwOutSize;					// size in bytes of output buffer set by plugin
	DWORD dwInterface;					// read only interface ID
	DWORD dwIndex;						// read only plugin index
}
PGIParamsBlock, *PPGIParamsBlock;

// void __stdcall PexRegisterPlugIn(LPCTSTR *);
typedef void (__stdcall* PexRegisterPlugIn)( LPCTSTR * );
//void __stdcall PexAboutPlugIn(void);
typedef void (__stdcall *PexAboutPlugIn) (void);
//BOOL __stdcall PexPreloadImage(PPGIParamsBlock);
typedef BOOL (__stdcall *PexPreloadImage)(PPGIParamsBlock);


#endif