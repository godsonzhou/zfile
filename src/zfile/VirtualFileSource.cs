using System;
using System.Collections.Generic;
using Files.FileSources;

namespace Files.FileSources
{
    public interface IVirtualFileSource : IFileSource
    {
    }

    public class VirtualFileSource : FileSource, IVirtualFileSource
    {
        private readonly object _syncRoot = new object();
        private readonly List<IFileSource> _virtualSources = new List<IFileSource>();

        public override Uri Uri => new Uri("virtual://");

        public override string FileSystem => "Virtual";

        public override string CurrentAddress => "virtual://";

        public override string CurrentWorkingDirectory => "virtual://";

        public override FilePropertyType SupportedFileProperties => FilePropertyType.Standard;

        public override FilePropertyType RetrievableFileProperties => FilePropertyType.Standard;

        public override bool IsOperational => true;

        public override void AddVirtualSource(IFileSource source)
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

        public override void RemoveVirtualSource(IFileSource source)
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

        public override IEnumerable<IFileSource> GetVirtualSources()
        {
            lock (_syncRoot)
            {
                return new List<IFileSource>(_virtualSources);
            }
        }
    }
}