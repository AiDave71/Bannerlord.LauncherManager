import { ModuleInfoExtended, ModuleInfoExtendedWithMetadata } from "./BannerlordModuleManager";

export interface INativeExtension {
  LauncherManager: new (
    setGameParametersAsync: (executable: string, gameParameters: string[]) => Promise<void>,
    sendNotificationAsync: (id: string, type: NotificationType, message: string, delayMS: number) => Promise<void>,
    sendDialogAsync: (type: DialogType, title: string, message: string, filters: FileFilter[]) => Promise<string>,
    getInstallPathAsync: () => Promise<string>,
    readFileContentAsync: (filePath: string, offset: number, length: number) => Promise<Uint8Array | null>,
    writeFileContentAsync: (filePath: string, data: Uint8Array) => Promise<void>,
    readDirectoryFileListAsync: (directoryPath: string) => Promise<string[] | null>,
    readDirectoryListAsync: (directoryPath: string) => Promise<string[] | null>,
    getAllModuleViewModelsAsync: () => Promise<ModuleViewModel[] | null>,
    getModuleViewModelsAsync: () => Promise<ModuleViewModel[] | null>,
    setModuleViewModelsAsync: (moduleViewModels: ModuleViewModel[]) => Promise<void>,
    getOptionsAsync: () => Promise<LauncherOptions>,
    getStateAsync: () => Promise<LauncherState>,
  ) => LauncherManager
}

export interface LoadOrderEntry {
  id: string;
  name: string;
  isSelected: boolean;
  isDisabled: boolean;
  index: number;
}

export interface LoadOrder {
  [id: string]: LoadOrderEntry;
}

export interface ModuleViewModel {
  moduleInfoExtended: ModuleInfoExtendedWithMetadata;
  isValid: boolean;
  isSelected: boolean;
  isDisabled: boolean;
  index: number;
}

export interface LauncherOptions {
  betaSorting: boolean;
}

export interface LauncherState {
  isSingleplayer: boolean;
}

export interface SaveMetadata {
  [key: string]: string;
  Name: string;
}

export type GameStore = 'Steam' | 'GOG' | 'Epic' | 'Xbox' | 'Unknown';
export type GamePlatform = 'Win64' | 'Xbox' | 'Unknown';

export type NotificationType = 'hint' | 'info' | 'warning' | 'error';
export type DialogType = 'warning' | 'fileOpen' | 'fileSave';

export type InstructionType = 'Copy' | 'ModuleInfo' | 'CopyStore';
export interface InstallInstruction {
  type: InstructionType;
  store?: GameStore;
  moduleInfo?: ModuleInfoExtended;
  source?: string;
  destination?: string;
}
export interface InstallResult {
  instructions: InstallInstruction[];
}

export interface SupportedResult {
  supported: boolean;
  requiredFiles: string[];
}

export interface OrderByLoadOrderResult {
  result: boolean;
  issues?: string[];
  orderedModuleViewModels?: ModuleViewModel[]
}

export interface FileFilter {
  name: string;
  extensions: string[];
}

// Save Editor Types
export type SaveEditorCompatibility = 'Compatible' | 'MinorIssues' | 'MajorIssues' | 'Incompatible' | 'Unknown';
export type SaveEntityType = 'Hero' | 'Party' | 'Settlement' | 'Clan' | 'Kingdom' | 'Fleet' | 'Ship' | 'Item' | 'Quest' | 'Workshop' | 'Caravan';
export type EditValidationSeverity = 'Info' | 'Warning' | 'Error' | 'Critical';

export interface SaveModuleInfo { id: string; version: string; }
export interface SaveHeader { headerVersion: number; gameVersionMajor: number; gameVersionMinor: number; gameVersionBuild: number; gameVersion: string; modules: SaveModuleInfo[]; }
export interface SaveMetadataInfo { characterName: string; clanName?: string; gameDay: number; gameYear: number; characterLevel: number; playtimeHours: number; saveTimestamp: string; }
export interface HeroAttributes { vigor: number; control: number; endurance: number; cunning: number; social: number; intelligence: number; total: number; }
export interface HeroSkillData { skillId: string; skillName: string; level: number; focus: number; maxLevel: number; }
export interface SaveHeroData { id: string; stringId: string; name: string; level: number; experience: number; gold: number; health: number; maxHealth: number; attributes: HeroAttributes; skills: HeroSkillData[]; perks: string[]; isMainHero: boolean; isDead: boolean; isPrisoner: boolean; clanId?: string; partyId?: string; }
export interface TroopStackData { troopId: string; troopName: string; count: number; woundedCount: number; tier: number; }
export interface SavePartyData { id: string; name: string; leaderId?: string; troops: TroopStackData[]; prisoners: TroopStackData[]; gold: number; totalTroops: number; totalPrisoners: number; }
export interface SaveFleetData { id: string; name: string; commanderId?: string; shipIds: string[]; currentPortId?: string; state: string; totalShips: number; }
export interface SaveShipData { id: string; name: string; shipType: string; currentHull: number; maxHull: number; crewCount: number; maxCrew: number; cargoCapacity: number; cargoUsed: number; upgrades: string[]; hullPercent: number; }
export interface EditValidation { severity: EditValidationSeverity; field: string; message: string; suggestion?: string; }
export interface EditResult { success: boolean; errorMessage?: string; validations: EditValidation[]; hasErrors: boolean; }
export interface SaveEditData { filePath: string; fileName: string; header: SaveHeader; metadata: SaveMetadataInfo; mainHero?: SaveHeroData; heroes: SaveHeroData[]; parties: SavePartyData[]; hasWarSails: boolean; fleets: SaveFleetData[]; ships: SaveShipData[]; checksum?: string; isModified: boolean; }
export interface SaveLoadOptions { permissive?: boolean; metadataOnly?: boolean; validateReferences?: boolean; }
export interface SaveWriteOptions { createBackup?: boolean; verifyAfterSave?: boolean; compressionLevel?: string; }
export interface HeroEditRequest { heroId: string; level?: number; experience?: number; gold?: number; health?: number; attributes?: HeroAttributes; skillLevels?: Record<string, number>; perksToAdd?: string[]; perksToRemove?: string[]; }
export interface PartyEditRequest { partyId: string; gold?: number; troopsToAdd?: Record<string, number>; troopsToRemove?: Record<string, number>; healAllWounded?: boolean; }
export interface ShipEditRequest { shipId: string; name?: string; currentHull?: number; crewCount?: number; upgradesToAdd?: string[]; upgradesToRemove?: string[]; repairFully?: boolean; }

export type LauncherManager = {
  constructor(): LauncherManager;

  checkForRootHarmonyAsync(): Promise<void>;
  getGamePlatformAsync(): Promise<GamePlatform>;
  getGameVersionAsync(): Promise<string>;
  getModulesAsync(): Promise<ModuleInfoExtendedWithMetadata[]>;
  getAllModulesAsync(): Promise<ModuleInfoExtendedWithMetadata[]>;
  getSaveFilePathAsync(saveFile: string): Promise<string>;
  getSaveFilesAsync(): Promise<SaveMetadata[]>;
  getSaveMetadataAsync(saveFile: string, data: Uint8Array): Promise<SaveMetadata>;
  installModule(files: string[], moduleInfos: ModuleInfoExtendedWithMetadata[]): InstallResult;
  isObfuscatedAsync(module: ModuleInfoExtendedWithMetadata): Promise<boolean>;
  isSorting(): boolean;
  moduleListHandlerExportAsync(): Promise<void>;
  moduleListHandlerExportSaveFileAsync(saveFile: string): Promise<void>;
  moduleListHandlerImportAsync(): Promise<boolean>;
  moduleListHandlerImportSaveFileAsync(saveFile: string): Promise<boolean>;
  orderByLoadOrderAsync(loadOrder: LoadOrder): Promise<OrderByLoadOrderResult>;
  refreshModulesAsync(): Promise<void>;
  refreshGameParametersAsync(): Promise<void>;
  setGameParameterExecutableAsync(executable: string): Promise<void>;
  setGameParameterSaveFileAsync(saveName: string): Promise<void>;
  setGameParameterContinueLastSaveFileAsync(value: boolean): Promise<void>;
  setGameStore(gameStore: GameStore): void;
  sortAsync(): Promise<void>;
  sortHelperChangeModulePositionAsync(moduleViewModel: ModuleViewModel, insertIndex: number): Promise<boolean>;
  sortHelperToggleModuleSelectionAsync(moduleViewModel: ModuleViewModel): Promise<ModuleViewModel>;
  sortHelperValidateModuleAsync(moduleViewModel: ModuleViewModel): Promise<string[]>;
  testModule(files: string[]): SupportedResult;

  dialogTestWarningAsync(): Promise<string>;
  dialogTestFileOpenAsync(): Promise<string>;

  setGameParameterLoadOrderAsync(loadOrder: LoadOrder): Promise<void>;

  // Save Editor methods
  loadSaveForEditAsync(savePath: string, options?: SaveLoadOptions): Promise<SaveEditData>;
  getCurrentSaveDataAsync(): Promise<SaveEditData | null>;
  saveEditedSaveAsync(targetPath?: string, options?: SaveWriteOptions): Promise<EditResult>;
  editHeroAsync(request: HeroEditRequest): Promise<EditResult>;
  editPartyAsync(request: PartyEditRequest): Promise<EditResult>;
  editShipAsync(request: ShipEditRequest): Promise<EditResult>;
  getSaveHeroesAsync(): Promise<SaveHeroData[]>;
  getSavePartiesAsync(): Promise<SavePartyData[]>;
  getSaveFleetsAsync(): Promise<SaveFleetData[]>;
  getSaveShipsAsync(): Promise<SaveShipData[]>;
  verifySaveIntegrityAsync(savePath: string): Promise<boolean>;
  createSaveBackupAsync(savePath: string): Promise<string>;
  closeSaveEditorAsync(): Promise<void>;
}
