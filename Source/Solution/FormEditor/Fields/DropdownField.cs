﻿namespace FormEditor.Fields
{
	public class DropdownField : FieldWithFieldValues
	{
		public DropdownField()
		{
			// default values
			FieldValues = new[] { new FieldValue { Value = "Value 1" }, new FieldValue { Value = "Value 2" } };
			DefaultText = "Select...";
		}
		public override string PrettyName => "Select box (drop-down)";

		public override string Type => "core.dropdown";
		public string DefaultText { get; set; }
	}
}
