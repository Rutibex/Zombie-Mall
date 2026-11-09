namespace Ashen.Core;

/// <summary>Product identity shared by the desktop host and headless checks.</summary>
public static class GameIdentity
{
 public const string Name="Zombie Mall";
 public const string ProfileDirectoryName="ZombieMall";
 public const string Version="0.33.2-phase33b";
 public const string WindowTitle="Zombie Mall - Phase 33B Lower Mall Environments";
 public const string CampaignLabel="ASHEN COVENANT ENGINE TEST";
 public static string ProfilePath=>Path.Combine(
  Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),ProfileDirectoryName);
}
