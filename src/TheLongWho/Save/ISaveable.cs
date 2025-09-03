namespace TheLongWho.Save
{
	public interface ISaveable
	{
		string SaveKey { get; }
		object GetSaveData();
		void LoadSaveData(object data);
	}
}
