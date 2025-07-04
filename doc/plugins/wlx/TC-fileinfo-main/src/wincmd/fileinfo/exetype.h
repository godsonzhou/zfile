/***** Format executable 
 MZ     old-style DOS executable (see #0881)
 NE     Windows or OS/2 1.x segmented ("new") executable (see #0883)
 LE     Windows virtual device driver (VxD) linear executable (see #0896)
 LX     variant of LE used in OS/2 2.x (see #0896)
 W3     Windows WIN386.EXE file; a collection of LE files
 PE     Win32 (Windows NT and Win32s) portable executable based on Unix COFF
 DL     HP 100LX/200LX system manager compliant executable (.EXM)
 MP     old PharLap .EXP (see #0906)
 P2     PharLap 286 .EXP (see #0907)
 P3     PharLap 386 .EXP (see #0907)
*****************/

#define  LZ  0x5A4C
#define  PK  0x4B50


#define IMAGE_DOS_INVSIGNATURE				0x4D5A      // ZM

#define IMAGE_LX_SIGNATURE					0x584C      // LX
#define IMAGE_LE_SIGNATURE					0x454C      // LE

#define  IMAGE_BW_SIGNATURE                 0x5742      // BW
#define  IMAGE_W3_SIGNATURE                 0x3357      // W3     Windows WIN386.EXE file; a collection of LE files
#define  IMAGE_W4_SIGNATURE                 0x3457      // W4     Windows  file; a collection of LE files
#define  IMAGE_DL_SIGNATURE                 0x4C44      // DL     HP 100LX/200LX system manager compliant executable (.EXM)
#define  IMAGE_MP_SIGNATURE                 0x504D      // MP     old PharLap .EXP (see #0906)
#define  IMAGE_P2_SIGNATURE                 0x3250      // P2     PharLap 286 .EXP (see #0907)
#define  IMAGE_P3_SIGNATURE                 0x3350      // P3     PharLap 386 .EXP (see #0907)
#define  IMAGE_PW_SIGNATURE                 0x4D50      // PMW1   packed with PMWLITE 

#define  IMAGE_OBJ_SIGNATURE                 0x4C10      