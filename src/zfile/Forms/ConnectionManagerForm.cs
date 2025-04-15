using Zfile.FileSources;
namespace Zfile.Forms
{
	public partial class ConnectionManagerForm : Form
	{
		private FileView _fileView;

		public ConnectionManagerForm()
		{
			InitializeComponent();
		}

		public ConnectionManagerForm(FileView fileView)
		{
			InitializeComponent();
			_fileView = fileView;
		}

		private void InitializeComponent()
		{
			this.tvConnections = new System.Windows.Forms.TreeView();
			this.gbConnectTo = new System.Windows.Forms.GroupBox();
			this.btnConnect = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.imageList = new System.Windows.Forms.ImageList();
			this.gbConnectTo.SuspendLayout();
			this.SuspendLayout();

			// tvConnections
			this.tvConnections.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tvConnections.Location = new System.Drawing.Point(3, 16);
			this.tvConnections.Name = "tvConnections";
			this.tvConnections.Size = new System.Drawing.Size(394, 231);
			this.tvConnections.TabIndex = 0;
			this.tvConnections.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvConnections_AfterSelect);

			// gbConnectTo
			this.gbConnectTo.Controls.Add(this.tvConnections);
			this.gbConnectTo.Location = new System.Drawing.Point(12, 12);
			this.gbConnectTo.Name = "gbConnectTo";
			this.gbConnectTo.Size = new System.Drawing.Size(400, 250);
			this.gbConnectTo.TabIndex = 0;
			this.gbConnectTo.TabStop = false;
			this.gbConnectTo.Text = "Connect to";

			// btnConnect
			this.btnConnect.Enabled = false;
			this.btnConnect.Location = new System.Drawing.Point(12, 268);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(75, 23);
			this.btnConnect.TabIndex = 1;
			this.btnConnect.Text = "Connect";
			this.btnConnect.UseVisualStyleBackColor = true;
			this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);

			// btnAdd
			this.btnAdd.Enabled = false;
			this.btnAdd.Location = new System.Drawing.Point(93, 268);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 2;
			this.btnAdd.Text = "Add";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);

			// btnEdit
			this.btnEdit.Enabled = false;
			this.btnEdit.Location = new System.Drawing.Point(174, 268);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.Size = new System.Drawing.Size(75, 23);
			this.btnEdit.TabIndex = 3;
			this.btnEdit.Text = "Edit";
			this.btnEdit.UseVisualStyleBackColor = true;
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);

			// btnDelete
			this.btnDelete.Enabled = false;
			this.btnDelete.Location = new System.Drawing.Point(255, 268);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Size = new System.Drawing.Size(75, 23);
			this.btnDelete.TabIndex = 4;
			this.btnDelete.Text = "Delete";
			this.btnDelete.UseVisualStyleBackColor = true;
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);

			// btnCancel
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(336, 268);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;

			// imageList
			this.imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.imageList.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;

			// ConnectionManagerForm
			this.AcceptButton = this.btnConnect;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(424, 301);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnDelete);
			this.Controls.Add(this.btnEdit);
			this.Controls.Add(this.btnAdd);
			this.Controls.Add(this.btnConnect);
			this.Controls.Add(this.gbConnectTo);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConnectionManagerForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Connection Manager";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ConnectionManagerForm_FormClosed);
			this.Load += new System.EventHandler(this.ConnectionManagerForm_Load);
			this.gbConnectTo.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		private void ConnectionManagerForm_Load(object sender, EventArgs e)
		{
			LoadWfxPlugins();
		}

		private void LoadWfxPlugins()
		{
			tvConnections.Nodes.Clear();

			// In a real implementation, this would load WFX plugins from a plugin manager
			// For example:
			// foreach (var plugin in WfxPluginManager.GetEnabledPlugins())
			// {
			//     var node = tvConnections.Nodes.Add(plugin.Name);
			//     node.Tag = new FileSourceRecord { FileSource = plugin.CreateFileSource() };
			//     node.ImageIndex = 0;
			//     
			//     // Load connections for this plugin
			//     int connectionIndex = 0;
			//     string connection;
			//     while (plugin.WfxModule.GetConnection(connectionIndex, out connection))
			//     {
			//         var childNode = node.Nodes.Add(connection);
			//         childNode.ImageIndex = 1;
			//         connectionIndex++;
			//     }
			// }
		}

		private void tvConnections_AfterSelect(object sender, TreeViewEventArgs e)
		{
			bool hasData = (e.Node?.Tag != null);
			btnConnect.Enabled = !hasData && e.Node?.Parent != null;
			btnAdd.Enabled = hasData;
			btnEdit.Enabled = !hasData && e.Node?.Parent != null;
			btnDelete.Enabled = !hasData && e.Node?.Parent != null;
		}

		private void btnConnect_Click(object sender, EventArgs e)
		{
			if (tvConnections.SelectedNode?.Parent == null) return;

			var parentNode = tvConnections.SelectedNode.Parent;
			if (parentNode.Tag is FileSourceRecord record)
			{
				var wfxFileSource = record.FileSource as IWfxPluginFileSource;
				if (wfxFileSource != null)
				{
					string connection = tvConnections.SelectedNode.Text;
					string rootPath = "";
					string remotePath = "";

					if (wfxFileSource.WfxModule.OpenConnection(connection, out rootPath, out remotePath))
					{
						// Normalize path separators
						rootPath = rootPath.Replace('\\', Path.DirectorySeparatorChar);
						remotePath = remotePath.Replace('\\', Path.DirectorySeparatorChar);

						// Set current address and root directory
						wfxFileSource.CurrentAddress = connection;
						wfxFileSource.RootDirectory = Path.GetFullPath(Path.Combine(rootPath, Path.DirectorySeparatorChar.ToString()));

						// Add file source to the file view
						_fileView.AddFileSource(wfxFileSource, Path.Combine(rootPath.TrimEnd(Path.DirectorySeparatorChar), remotePath));

						// Clear parent node tag to indicate connection is active
						parentNode.Tag = null;

						DialogResult = DialogResult.OK;
						Close();
					}
					else
					{
						MessageBox.Show($"Cannot connect to {connection}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			if (tvConnections.SelectedNode?.Tag is FileSourceRecord record)
			{
				var wfxFileSource = record.FileSource as IWfxPluginFileSource;
				if (wfxFileSource != null)
				{
					string connection = "";
					if (wfxFileSource.WfxModule.ManageConnection(this.Handle, ref connection, WfxConstants.FS_NM_ACTION_ADD))
					{
						var childNode = tvConnections.SelectedNode.Nodes.Add(connection);
						childNode.ImageIndex = 1;
					}
				}
			}
		}

		private void btnEdit_Click(object sender, EventArgs e)
		{
			if (tvConnections.SelectedNode?.Parent == null) return;

			var parentNode = tvConnections.SelectedNode.Parent;
			if (parentNode.Tag is FileSourceRecord record)
			{
				var wfxFileSource = record.FileSource as IWfxPluginFileSource;
				if (wfxFileSource != null)
				{
					string connection = tvConnections.SelectedNode.Text;
					if (wfxFileSource.WfxModule.ManageConnection(this.Handle, ref connection, WfxConstants.FS_NM_ACTION_EDIT))
					{
						tvConnections.SelectedNode.Text = connection;
					}
				}
			}
		}

		private void btnDelete_Click(object sender, EventArgs e)
		{
			if (tvConnections.SelectedNode?.Parent == null) return;

			var parentNode = tvConnections.SelectedNode.Parent;
			if (parentNode.Tag is FileSourceRecord record)
			{
				var wfxFileSource = record.FileSource as IWfxPluginFileSource;
				if (wfxFileSource != null)
				{
					string connection = tvConnections.SelectedNode.Text;
					if (wfxFileSource.WfxModule.ManageConnection(this.Handle, ref connection, WfxConstants.FS_NM_ACTION_DELETE))
					{
						tvConnections.Nodes.Remove(tvConnections.SelectedNode);
					}
				}
			}
		}

		private void ConnectionManagerForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			// Clean up resources
			foreach (TreeNode node in tvConnections.Nodes)
			{
				if (node.Tag is FileSourceRecord record)
				{
					// In a real implementation, this would dispose of file source resources
					// record.FileSource?.Dispose();
				}
			}
		}

		private System.Windows.Forms.TreeView tvConnections;
		private System.Windows.Forms.GroupBox gbConnectTo;
		private System.Windows.Forms.Button btnConnect;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnEdit;
		private System.Windows.Forms.Button btnDelete;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.ImageList imageList;
	}

	public static class ConnectionManagerHelper
	{
		public static bool ShowConnectionManager(FileView fileView)
		{
			using (var form = new ConnectionManagerForm(fileView))
			{
				return form.ShowDialog() == DialogResult.OK;
			}
		}
	}
}