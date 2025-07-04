typedef struct _PDB_INFO {
    CHAR    Signature[4];   // "NBxx"
    ULONG   Offset;         // always zero
    ULONG   sig;
    ULONG   age;
    CHAR    PdbName[_MAX_PATH];
} PDB_INFO, *PPDB_INFO;

typedef struct _RSDS_INFO {
    CHAR    Signature[4];   // "RSDS"
    ULONG   guid1;
    ULONG   guid2;
    ULONG   guid3;
    ULONG   guid4;
    ULONG   age;
    CHAR    PdbName[_MAX_PATH];
} RSDS_INFO, *PRSDS_INFO;
