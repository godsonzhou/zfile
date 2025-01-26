using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace Sheng.Winform.Controls
{
    
    public class ShengDatetimePicker:DateTimePicker
    {
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

        private string relationType;
        /// <summary>
        /// �������ͣ�Start��End�����
        /// </summary>
        [Description("�������ͣ�Start��End�����")]
        [Category("Sheng.Winform.Controls")]
        public string RelationType
        {
            get
            {
                return this.relationType;
            }
            set
            {
                this.relationType = value;
            }
        }

        private ShengDatetimePicker relation;
        /// <summary>
        /// ��������
        /// </summary>
        [Description("��������")]
        [Category("Sheng.Winform.Controls")]
        public ShengDatetimePicker Relation
        {
            get
            {
                return this.relation;
            }
            set
            {
                this.relation = value;
            }
        }

        public ShengDatetimePicker()
        {
            
        }

        /// <summary>
        /// �ı�ѡ�������ʱ,�Թ���������в���
        /// </summary>
        /// <param name="eventargs"></param>
        protected override void OnValueChanged(EventArgs eventargs)
        {
            base.OnValueChanged(eventargs);
        }

    }
}
