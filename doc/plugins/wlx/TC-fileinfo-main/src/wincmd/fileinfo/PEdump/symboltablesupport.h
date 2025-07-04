#include <afx.h>

class COFFSymbolTable;

BOOL LookupSymbolName(DWORD index, PSTR buffer, UINT length);
CString DumpSymbolTable( COFFSymbolTable * pSymTab );
CString DumpMiscDebugInfo( PIMAGE_DEBUG_MISC PMiscDebugInfo );
CString DumpCVDebugInfo( PDWORD pCVHeader );
CString DumpLineNumbers(PIMAGE_LINENUMBER pln, DWORD count);

