#if !defined( __tcoptions_H )
#define __tcoptions_H

   struct OPTIONS 
   {
		BOOL autosave;
		BOOL rememberAP;
		BOOL undec;
		BOOL res;
		BOOL debug;
		BYTE MaxDepth;
		BOOL sort;
		BOOL pdata;
		BOOL reloc;
		char *p_ini;
		char tlexc[256];
   };

#endif
