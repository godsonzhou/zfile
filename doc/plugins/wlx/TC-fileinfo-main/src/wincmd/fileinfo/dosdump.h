CString DumpMZHeader( PIMAGE_DOS_HEADER pdosHeader );
CString DumpNEHeader( PIMAGE_DOS_HEADER dosHeader );
//CString DumpLXHeader( PIMAGE_DOS_HEADER dosHeader );
CString DumpLXHeader( EXE_FILE *pEXE );
//CString DumpLEHeader( PIMAGE_DOS_HEADER pdosHeader );
CString DumpLEHeader( EXE_FILE *pEXE );