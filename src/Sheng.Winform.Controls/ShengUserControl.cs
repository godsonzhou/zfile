using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using Sheng.Winform.Controls.Localisation;

namespace Sheng.Winform.Controls
{
    
    public partial class ShengUserControl : UserControl, IShengValidate
    {
        #region ����

        public ShengUserControl()
        {

            InitializeComponent();
        }

        #endregion

        #region ��������

        /// <summary>
        /// ��֤�ؼ�
        /// ������֤,�����д�������������ʾ,���ڲ���Form�е�����֤�����ĵط�
        /// ʹ���򵼵����
        /// </summary>
        /// <returns></returns>
        public virtual bool DoValidate()
        {
            bool validateResult = true;
            string validateMsg;

            validateResult = this.SEValidate(out validateMsg);
            if (validateResult == false)
            {
                MessageBox.Show(validateMsg, Language.Current.MessageBoxCaptiton_Message, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return validateResult;
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
        public virtual bool SEValidate(out string validateMsg)
        {
            return ShengValidateHelper.ValidateContainerControl(this, out validateMsg);
        }

        public CustomValidateMethod CustomValidate
        {
            get;
            set;
        }


        #endregion
    }
}
