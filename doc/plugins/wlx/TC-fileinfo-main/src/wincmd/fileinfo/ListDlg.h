#if !defined(AFX_LISTDLG_H__4BA6DD0E_DF32_410D_8A5F_91F558F44648__INCLUDED_)
#define AFX_LISTDLG_H__4BA6DD0E_DF32_410D_8A5F_91F558F44648__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// ListDlg.h : header file
//
#include "resource.h"
/////////////////////////////////////////////////////////////////////////////
// ListDlg dialog

class ListDlg : public CDialog
{
// Construction
public:
	ListDlg(CStringList *pstrl,CWnd* pParent = NULL);   // standard constructor

// Dialog Data
	//{{AFX_DATA(ListDlg)
	enum { IDD = IDD_NotFound };
	CListBox	m_list;
	//}}AFX_DATA
	CStringList   *m_pstrl;

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(ListDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:

	// Generated message map functions
	//{{AFX_MSG(ListDlg)
	virtual BOOL OnInitDialog();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_LISTDLG_H__4BA6DD0E_DF32_410D_8A5F_91F558F44648__INCLUDED_)
