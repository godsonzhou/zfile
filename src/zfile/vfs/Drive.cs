using System;
using System.Collections.Generic;

namespace zfile
{
    /// <summary>
    /// Enumeration of drive types
    /// </summary>
    public enum DriveType
    {
        Unknown,
        Flash,          // Flash drive
        Floppy,         // 3.5'', ZIP drive, etc.
        HardDisk,       // Hard disk drive
        Network,        // Network share
        Optical,        // CD, DVD, Blu-Ray, etc.
        RamDisk,        // Ram-disk
        Removable,      // Drive with removable media
        RemovableUsb,   // Drive connected via USB
        Virtual,        // Virtual drive
        Special         // Special drive
    }

    /// <summary>
    /// Represents a drive in the system
    /// </summary>
    public class Drive
    {
        /// <summary>
        /// Name displayed to the user
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Where this drive is or should be mounted
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Drive label if filesystem on the drive supports it
        /// </summary>
        public string DriveLabel { get; set; } = string.Empty;

        /// <summary>
        /// Device ID that can be used for mounting, ejecting, etc.
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Type of the drive
        /// </summary>
        public DriveType DriveType { get; set; } = DriveType.Unknown;

        /// <summary>
        /// Drive size
        /// </summary>
        public long DriveSize { get; set; } = 0;

        /// <summary>
        /// Filesystem on the drive
        /// </summary>
        public string FileSystem { get; set; } = string.Empty;

        /// <summary>
        /// Is media available in a drive with removable media
        /// </summary>
        public bool IsMediaAvailable { get; set; } = false;

        /// <summary>
        /// Can eject media by a command
        /// </summary>
        public bool IsMediaEjectable { get; set; } = false;

        /// <summary>
        /// If the drive has removable media
        /// </summary>
        public bool IsMediaRemovable { get; set; } = false;

        /// <summary>
        /// Is the drive mounted
        /// </summary>
        public bool IsMounted { get; set; } = false;

        /// <summary>
        /// Should the drive be automounted
        /// </summary>
        public bool AutoMount { get; set; } = false;
    }

    /// <summary>
    /// A list of drives in the system
    /// </summary>
    public class DrivesList
    {
        private readonly List<Drive> _list = new List<Drive>();

        /// <summary>
        /// Gets the number of drives in the list
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        /// Gets the drive at the specified index
        /// </summary>
        /// <param name="index">The index of the drive to get</param>
        /// <returns>The drive at the specified index</returns>
        public Drive this[int index] => _list[index];

        /// <summary>
        /// Adds a drive to the list
        /// </summary>
        /// <param name="drive">The drive to add</param>
        /// <returns>The index at which the drive was added</returns>
        public int Add(Drive drive)
        {
            _list.Add(drive);
            return _list.Count - 1;
        }

        /// <summary>
        /// Removes a drive at the specified index
        /// </summary>
        /// <param name="index">The index of the drive to remove</param>
        public void Remove(int index)
        {
            if (index >= 0 && index < _list.Count)
            {
                _list.RemoveAt(index);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid index");
            }
        }

        /// <summary>
        /// Removes all drives from the list
        /// </summary>
        public void RemoveAll()
        {
            _list.Clear();
        }

        /// <summary>
        /// Sorts the drives in the list using the specified comparison
        /// </summary>
        /// <param name="comparison">The comparison to use for sorting</param>
        public void Sort(Comparison<Drive> comparison)
        {
            _list.Sort(comparison);
        }
    }
}
