using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Sheng.Winform.Controls.Drawing;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.ComponentModel;

namespace Sheng.Winform.Controls
{
    
    public class ShengToolStripMenuItem : ToolStripMenuItem
    {

        StringFormat stringFormat = new StringFormat();

        public ShengToolStripMenuItem()
            : this(String.Empty)
        {
        }

        public ShengToolStripMenuItem(string strName)
            : base(strName)
        {

            stringFormat.HotkeyPrefix = HotkeyPrefix.Show;
            //stringFormat.Trimming = StringTrimming.None;
            //stringFormat.LineAlignment = StringAlignment.Near;

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color colorMenuHighLight = ControlPaint.LightLight(SystemColors.MenuHighlight);

            #region ����

            //��ʾͼ���λ��X����
            int imageLocationX = 4;

            //��ʾͼ���λ��Y����
            int imageLocationY = 2;

            //��ʾ�ı���λ��X����
            int textLocationX = 6;

            //��ʾ�ı���λ��Y����
            //int textLocationY = 4;
            int textLocationY = (int)Math.Round((float)(this.ContentRectangle.Height - (int)Math.Round(this.Font.SizeInPoints)) / 2);

            //�Ӳ˵���ʾ�ı�λ��X����
            int textLocationX_DropDown = 33;

            //�Ӳ˵���ʾͼ���λ��X����
            int imageLocationX_DropDown = 5;

            //�Ӳ˵���ʾͼ���λ��Y����
            int imageLocationY_DropDown = 3;

            //�ı����
            SolidBrush textBrush = new SolidBrush(this.ForeColor);

            //��ʾͼ���Rectangle
            Rectangle imageRect = new Rectangle(imageLocationX, imageLocationY, 16, 16);

            //�Ӳ˵���ʾͼ���Rectangle
            Rectangle imageRect_DropDown = new Rectangle(imageLocationX_DropDown, imageLocationY_DropDown, 16, 16);

            //�˵��������
            SolidBrush backBrush_Normal = new SolidBrush(SystemColors.ControlLightLight);

            //�˵�������� ѡ��״̬
            //LinearGradientBrush backBrush_Selected = new LinearGradientBrush(new Point(0, 0), new Point(0, this.Height),
            //            Color.FromArgb(255, 246, 204), Color.FromArgb(255, 194, 115));
            LinearGradientBrush backBrush_Selected = new LinearGradientBrush(new Point(0, 0), new Point(0, this.Height),
                      Color.White, colorMenuHighLight);


            //�˵�������� ����״̬
            //LinearGradientBrush backBrush_Pressed = new LinearGradientBrush(new Point(0, 0), new Point(0, this.Height),
            //            Color.White, Color.LightSkyBlue);
            LinearGradientBrush backBrush_Pressed = new LinearGradientBrush(new Point(0, 0), new Point(0, this.Height),
                        Color.White, colorMenuHighLight);

            //�Ӳ˵������������
            LinearGradientBrush leftBrush_DropDown = new LinearGradientBrush(new Point(0, 0), new Point(25, 0),
                        Color.White, Color.FromArgb(233, 230, 215));

            //���Rectangle ����
            Rectangle fillRect = new Rectangle(0, 0, this.Bounds.Width, this.Bounds.Height);

            //�Ӳ˵����Rectangle
            Rectangle fillRect_DropDown = new Rectangle(2, 0, this.Bounds.Width - 4, this.Bounds.Height);

            //�߿�Rectangle ����
            Rectangle drawRect = new Rectangle(0, 0, this.Bounds.Width - 1, this.Bounds.Height - 1);

            //�Ӳ˵��߿�Rectangle
            Rectangle drawRect_DropDown = new Rectangle(3, 0, this.Bounds.Width - 6, this.Bounds.Height - 2);

            //�Ӳ˵���������ݵķָ���
            Pen leftLine = new Pen(Color.FromArgb(197, 194, 184));

            //�߿򻭱� ����
            //Pen drawPen = new Pen(Color.FromArgb(255, 192, 111));
            Pen drawPen = new Pen(SystemColors.GradientActiveCaption);

            //����ʱ�ı߿򻭱� ����
            //Pen drawPen_Pressed = new Pen(Color.SkyBlue);
            Pen drawPen_Pressed = new Pen(SystemColors.GradientActiveCaption);

            //�߿򻭱�
            //Pen drawPen_DropDown = new Pen(Color.FromArgb(255, 192, 111));
            Pen drawPen_DropDown = new Pen(SystemColors.GradientActiveCaption);

            #endregion

            #region ���������,���ı����ɫ�ĳɻ�ɫ,ͼƬ�ҵ�

            //���������,���ı����ɫ�ĳɻ�ɫ
            if (this.Enabled)
            {
                textBrush.Color = this.ForeColor;
            }
            else
            {
                textBrush.Color = Color.LightGray;
            }

            #endregion

            #region ����˵�

            //����Ƕ���˵�
            if (!this.IsOnDropDown)
            {
                //����ǰ���״̬
                if (this.Pressed)
                {
                    e.Graphics.FillRectangle(backBrush_Pressed, fillRect);
                    e.Graphics.DrawRectangle(drawPen_Pressed, drawRect);
                }
                //�����ѡ��״̬
                else if (this.Selected)
                {
                    e.Graphics.FillRectangle(backBrush_Selected, fillRect);
                    e.Graphics.DrawRectangle(drawPen, drawRect);
                }

                //����ͼ����ı�
                if (this.Image != null)
                {
                    if (this.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText)
                    {
                        if (this.Enabled)
                            e.Graphics.DrawImage(this.Image, imageRect);
                        else
                            ControlPaint.DrawImageDisabled(e.Graphics, this.Image, imageRect.X, imageRect.Y, this.BackColor);

                        e.Graphics.DrawString(this.Text, this.Font, textBrush, new Point(textLocationX + 14, textLocationY), stringFormat);
                    }
                    else if (this.DisplayStyle == ToolStripItemDisplayStyle.Image)
                    {
                        if (this.Enabled)
                            e.Graphics.DrawImage(this.Image, imageRect);
                        else
                            ControlPaint.DrawImageDisabled(e.Graphics, this.Image, imageRect.X, imageRect.Y, this.BackColor);
                    }
                    else if (this.DisplayStyle == ToolStripItemDisplayStyle.Text)
                    {
                        e.Graphics.DrawString(this.Text, this.Font, textBrush, new Point(textLocationX, textLocationY), stringFormat);
                    }
                }
                else
                {
                    e.Graphics.DrawString(this.Text, this.Font, textBrush, new Point(textLocationX, textLocationY), stringFormat);
                }

            }

            #endregion

            #region ������Ƕ���˵�

            //������Ƕ���˵�
            else
            {
                #region �����ѡ�л��ǰ���״̬

                //�����ѡ�л��ǰ���״̬
                if (this.Selected || this.Pressed)
                {
                    //e.Graphics.FillRectangle(backBrush_Selected,fillRect_DropDown);

                    e.Graphics.FillRectangle(backBrush_Normal, fillRect_DropDown);
                    e.Graphics.FillRectangle(leftBrush_DropDown, 0, 0, 25, this.Height);
                    e.Graphics.DrawLine(leftLine, 25, 0, 25, this.Height);

                    //�������
                    if (this.Enabled)
                    {
                        //GraphPlotting.FillRoundRect(e.Graphics, backBrush_Selected, drawRect_DropDown, 0, 2);
                        //GraphPlotting.DrawRoundRect(e.Graphics, drawPen_DropDown, drawRect_DropDown, 2);                      

                        e.Graphics.FillPath(backBrush_Selected, DrawingTool.RoundedRect(drawRect_DropDown, 3));
                        e.Graphics.DrawPath(drawPen_DropDown, DrawingTool.RoundedRect(drawRect_DropDown, 3));
                    }

                    if (this.Image != null)
                    {
                        //�Ӳ˵��������д��һ��
                        //��Ϊ������û��ͼ,�ı���λ���ǲ����
                        if (this.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText ||
                            this.DisplayStyle == ToolStripItemDisplayStyle.Image
                            )
                        {
                            if (this.Enabled)
                                e.Graphics.DrawImage(this.Image, imageRect_DropDown);
                            else
                                ControlPaint.DrawImageDisabled(e.Graphics, this.Image, imageRect_DropDown.X, imageRect_DropDown.Y,this.BackColor);
                        }
                    }

                    e.Graphics.DrawString(this.Text, this.Font, textBrush, new Point(textLocationX_DropDown, textLocationY), stringFormat);

                }

                #endregion

                #region ���δѡ��Ҳδ����

                //���δѡ��Ҳδ����
                else
                {

                    e.Graphics.FillRectangle(backBrush_Normal, fillRect_DropDown);
                    e.Graphics.FillRectangle(leftBrush_DropDown, 0, 0, 25, this.Height);
                    e.Graphics.DrawLine(leftLine, 25, 0, 25, this.Height);

                    if (this.Image != null)
                    {
                        if (this.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText ||
                            this.DisplayStyle == ToolStripItemDisplayStyle.Image)
                        {
                            if (this.Enabled)
                                e.Graphics.DrawImage(this.Image, imageRect_DropDown);
                            else
                                ControlPaint.DrawImageDisabled(e.Graphics, this.Image, imageRect_DropDown.X, imageRect_DropDown.Y, this.BackColor);
                        }
                    }

                    e.Graphics.DrawString(this.Text, this.Font, textBrush, new Point(textLocationX_DropDown, textLocationY), stringFormat);

                }

                #endregion

                #region ����Checked = true
 
         //       ControlPaint.draw
             //   MenuGlyph.

                if (this.Checked)
                {
                    ControlPaint.DrawMenuGlyph
                        (e.Graphics, imageLocationX_DropDown, imageLocationY_DropDown, 16, 16, 
                        MenuGlyph.Checkmark,Color.Black, SystemColors.GradientActiveCaption);
                }

                #endregion

                #region ��������Ӳ˵�,�����Ҽ�ͷ

                if (this.DropDownItems.Count > 0)
                {
                    ControlPaint.DrawMenuGlyph
                        (e.Graphics,this.Width - 20, imageLocationY_DropDown, 16, 16,
                        MenuGlyph.Arrow, Color.Black, Color.Transparent);
                }

                #endregion
            }

            #endregion
           

            #region �ͷ���Դ

            //�ͷ���Դ
            textBrush.Dispose();
            backBrush_Normal.Dispose();
            backBrush_Selected.Dispose();
            backBrush_Pressed.Dispose();
            leftBrush_DropDown.Dispose();
            leftLine.Dispose();
            drawPen.Dispose();
            drawPen_Pressed.Dispose();
            drawPen_DropDown.Dispose();

            #endregion
        }

    }
}
