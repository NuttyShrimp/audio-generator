﻿using System.Reflection;
using AudioGenerator.Model.DataManifest;
using AudioGenerator.Util;
using CodeWalker.GameFiles;
using Newtonsoft.Json;
using static AudioGenerator.Model.Generic;
namespace AudioGenerator.Model.Dat54;

public class Generator
{
    private static Dictionary<string, int> flags = new()
    {
        {"Volume", 0x00000004},
        {"DopplerFactor", 0x00004000},
        {"Category", 0x00008000},
        {"RolloffCurve", 0x00100000},
        {"DistanceAttenuation", 0x00200000},
        {"Unk20", 0x00800000},
        {"Unk22", 0x08000000},
        {"Unk23", 0x10000000},
        {"Unk24", 0x20000000},
    };

    private static Value CalcHeaderFlag(SoundHeader header)
    {
        int flag = 0x00000000;
        if (header.Category != null) { flag += flags["Category"]; }
        if (header.Volume != null) { flag += flags["Volume"]; }
        if (header.DopplerFactor != null) { flag += flags["DopplerFactor"]; }
        if (header.RolloffCurve != null) { flag += flags["RolloffCurve"]; }
        if (header.DistanceAttenuation != null) { flag += flags["DistanceAttenuation"]; }
        if (header.Unk20 != null) { flag += flags["Unk20"]; }
        if (header.Unk22 != null) { flag += flags["Unk22"]; }
        if (header.Unk23 != null) { flag += flags["Unk23"]; }
        if (header.Unk24 != null) { flag += flags["Unk24"]; }
        return CreateValue($"0x{flag.ToString("X8")}");
    }
    private static SoundItem GetSimpleSound(string name, string rpfPath, int volume)
    {
        SoundHeader header = new()
        {
            Volume = CreateValue($"{volume}"),
        };
        header.Flags = CalcHeaderFlag(header);
        return new SoundItem()
        {
            type = "SimpleSound",
            Name = name,
            Header = header,
            ContainerName = rpfPath,
            FileName = name,
            WaveSlotNum = CreateValue("0")
        };
    }

    private static SoundItem GetStereoSound(string name, int volume)
    {
        SoundHeader header = new()
        {
            Volume = CreateValue($"{volume}"),
        };
        header.Flags = CalcHeaderFlag(header);
        return new SoundItem()
        {
            type = "CollapsingStereoSound",
            Name = $"{name}_css",
            Header = header,
            ChildSound1 = $"{name}_l",
            ChildSound2 = $"{name}_r",
            UnkFloat0 = CreateValue("0"),
            UnkFloat1 = CreateValue("0"),
            ParameterHash0 = "",
            ParameterHash1 = "",
            ParameterHash2 = "",
            ParameterHash3 = "",
            ParameterHash4 = "",
            ParameterHash5 = "",
            UnkInt = CreateValue("1065353216"),
            UnkByte = CreateValue("0")
        };
    }

    private static SoundItem GetLoopingSound(string name, int volume)
    {
        SoundHeader header = new()
        {
            Volume = CreateValue($"{volume}"),
        };
        header.Flags = CalcHeaderFlag(header);
        return new SoundItem()
        {
            type = "LoopingSound",
            Name = $"{name}_loop",
            Header = header,
            LoopCount = CreateValue("-1"),
            LoopCountVariance = CreateValue("0"),
            UnkShort2 = CreateValue("0"),
            ChildSound = $"{name}_css",
            LoopCountParameter = "",
        };
    }

    private static SoundItem GetMultitrackSound(string name, baseSoundHeader entryHeaders, bool isLooped)
    {
        SoundHeader header = new()
        {
            Unk20 = CreateValue("0")
        };
        foreach (var property in entryHeaders.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance))
        {
            if (property.Name.Equals("volume")) { header.Volume = CreateValue($"{property.GetValue(entryHeaders)}"); }
            if (property.Name.Equals("dopplerFactor")) { header.DopplerFactor = CreateValue($"{property.GetValue(entryHeaders)}"); }
            if (property.Name.Equals("distance")) { header.DistanceAttenuation = CreateValue($"{property.GetValue(entryHeaders)}"); }
            if (property.Name.Equals("echox")) { header.Unk22 = CreateValue($"{property.GetValue(entryHeaders)}"); }
            if (property.Name.Equals("echoy")) { header.Unk23 = CreateValue($"{property.GetValue(entryHeaders)}"); }
            if (property.Name.Equals("echoz")) { header.Unk24 = CreateValue($"{property.GetValue(entryHeaders)}"); }
            // TODO ramove hash & replace with frontend_game_nofade
            if (property.Name.Equals("categoryHash"))
            {
                header.Category = $"hash_{property.GetValue(entryHeaders)}";
            }
            // TODO Same as above but for rolloffcurve
            if (property.Name.Equals("rolloffHash"))
            {
                header.RolloffCurve = $"hash_{property.GetValue(entryHeaders)}";
            }
        }
        header.Flags = CalcHeaderFlag(header);
        string childSoundAppendix = isLooped ? "loop" : "css";
        return new SoundItem()
        {
            type = "MultitrackSound",
            Name = $"{name}_mt",
            Header = header,
            ChildSounds = new []
            {
                $"{name}_{childSoundAppendix}",
            }
        };
    }

    private static SoundItem GetSoundSet(FileEntry[] files, string rpfName)
    {
        List<SetItem> setItems = new();
        List<int> itemHash = new();
        // JenkHash sorting in Scriptname dlcName_songName
        int GetListIdx(int hash)
        {
            int idx = 0;
            bool found = false;
            while (!found && idx < itemHash.Count)
            {
                int comparingHash = itemHash.ElementAt(idx);
                if (hash < comparingHash)
                {
                    found = true;
                }
                idx++;
            }
            return idx;
        }
        foreach (FileEntry file in files)
        {
            string scriptName = Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(file.name));
            JenkHash hash = new(scriptName, JenkHashInputEncoding.UTF8);
            int idx = GetListIdx(hash.HashInt);
            setItems.Insert(idx, new()
            {
                ScriptName = scriptName,
                ChildSound = $"{scriptName}_mt"
            });
            itemHash.Insert(idx, hash.HashInt);
        }
        SoundItem soundSet = new SoundItem()
        {
            type = "SoundSet",
            Name = rpfName.Replace("/", "_"),
            Header = new()
            {
                Flags = CreateValue("0xAAAAAAAA")
            },
            Items = setItems.ToArray()
        };
        return soundSet;
    }
    public static void GenerateDat54Xml(string path, string rpfPath, FileEntry[] files)
    {
        Dat54 dat54 = new Dat54();
        dat54.Version = CreateValue("7314721");
        dat54.ContainerPaths = new[] {rpfPath};
        List<SoundItem> simpleItems = new();
        List<SoundItem> StereoItems = new();
        List<SoundItem> LoopingItems = new();
        List<SoundItem> MultitrackItems = new();
        // Generate all simple Sounds
        foreach (FileEntry file in files)
        {
            string trimmedName = Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(file.name));
            simpleItems.Add(GetSimpleSound($"{trimmedName}_l", rpfPath, file.headers.volume));
            simpleItems.Add(GetSimpleSound($"{trimmedName}_r", rpfPath, file.headers.volume));
            StereoItems.Add(GetStereoSound(trimmedName, file.headers.volume));
            if (file.looped)
            {
                LoopingItems.Add(GetLoopingSound(trimmedName, file.headers.volume));
            }
            MultitrackItems.Add(GetMultitrackSound(trimmedName, file.headers, file.looped));
        }

        List<SoundItem> items = simpleItems.Concat(StereoItems).Concat(LoopingItems).Concat(MultitrackItems).ToList();
        items.Add(GetSoundSet(files, rpfPath));
        dat54.Items = items.ToArray();
        Xml.WriteXml(path, dat54);
    }
}