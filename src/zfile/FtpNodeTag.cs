namespace zfile
{
    /// <summary>
    /// FTP节点标签类，用于存储FTP节点的相关信息
    /// </summary>
    public class FtpNodeTag
    {
        /// <summary>
        /// 获取或设置FTP连接名称
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        /// 获取或设置FTP路径
        /// </summary>
        public string Path { get; set; }
    }
	public class FtpRootNodeTag(string name)
	{
		public string Name { get; set; } = name;
	
	}
}