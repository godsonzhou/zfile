using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using System.ComponentModel;

namespace Sheng.Winform.Controls
{
    
    public class ShengTabControl : TabControl, IShengValidate
    {
        #region ����

        public ShengTabControl()
        {
            
        }

        #endregion

        #region ISEValidate ��Ա

        private string title;
        /// <summary>
        /// ����
        /// </summary>
        [Description("����")]
        [Category("Sheng.Winform.Controls")]
        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                this.title = value;
            }
        }

        private bool highLight = false;
        /// <summary>
        /// ��֤ʧ��ʱ�Ƿ���Ҫ������ʾ���ı䱳��ɫ��
        /// </summary>
        [Description("��֤ʧ��ʱ�Ƿ���Ҫ������ʾ���ı䱳��ɫ��")]
        [Category("Sheng.Winform.Controls")]
        public bool HighLight
        {
            get
            {
                return this.highLight;
            }
            set
            {
                this.highLight = value;
            }
        }

        /// <summary>
        /// ��֤�ؼ�
        /// </summary>
        /// <param name="validateMsg"></param>
        /// <returns></returns>
        public bool SEValidate(out string validateMsg)
        {
            bool validateResult = true;
            string tabValidateMsg;
            validateMsg = String.Empty;

            foreach (TabPage tabPage in this.TabPages)
            {
                if (ShengValidateHelper.ValidateContainerControl(tabPage, out tabValidateMsg) == false)
                {
                    validateMsg += tabValidateMsg;
                    validateResult = false;
                }
            }

            return validateResult;
        }

        public CustomValidateMethod CustomValidate
        {
            get;
            set;
        }

        #endregion
    }
}
