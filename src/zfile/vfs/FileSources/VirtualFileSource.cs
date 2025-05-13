namespace zfile
{
    public interface IVirtualFileSource : IFileSource
    {
    }

    public abstract class VirtualFileSource : FileSource, IVirtualFileSource
    {
        private readonly object _syncRoot = new object();
        private readonly List<IFileSource> _virtualSources = new List<IFileSource>();

        public override Uri Uri => new Uri("virtual://");

        public override string FileSystem => "Virtual";

        public override string CurrentAddress => "virtual://";

        public override string CurrentWorkingDirectory => "virtual://";

		public override FilePropertiesTypes SupportedFileProperties => FilePropertiesTypes.Standard;

		public override FilePropertiesTypes RetrievableFileProperties => FilePropertiesTypes.Standard;

		public bool IsOperational => true;

		public void AddVirtualSource(IFileSource source)
		{
			if (source == null)
				return;

			lock (_syncRoot)
			{
				if (!_virtualSources.Contains(source))
				{
					_virtualSources.Add(source);
					source.ParentFileSource = this;
				}
			}
		}

		public void RemoveVirtualSource(IFileSource source)
		{
			if (source == null)
				return;

			lock (_syncRoot)
			{
				if (_virtualSources.Contains(source))
				{
					_virtualSources.Remove(source);
					source.ParentFileSource = null;
				}
			}
		}

		public IEnumerable<IFileSource> GetVirtualSources()
		{
			lock (_syncRoot)
			{
				return new List<IFileSource>(_virtualSources);
			}
		}
	}
}