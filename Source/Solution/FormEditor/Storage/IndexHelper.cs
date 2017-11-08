﻿using System;
using System.Configuration;

namespace FormEditor.Storage
{
	public class IndexHelper
	{
		public static IIndex GetIndex(int contentId)
		{
			if (Configuration.Instance.IndexType != null)
			{
				try
				{
					if (!(Activator.CreateInstance(Configuration.Instance.IndexType, contentId) is IIndex index))
					{
						throw new ConfigurationErrorsException($"Activator was unable to instantiate the custom Index type \"{Configuration.Instance.IndexType.AssemblyQualifiedName}\"");
					}
					return index;
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Could not create an instance of the custom Index type");
				}
			}
			// revert to default index
			return new Index(contentId);
		}
	}
}
