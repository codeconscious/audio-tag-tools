module Shared.IO

open System
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, string> =
    try
        fileInfo.FullName
        |> File.ReadAllText
        |> Ok
    with ex -> Error ex.Message

let readLines (fileInfo: FileInfo) : Result<string array, string> =
    try
        fileInfo.FullName
        |> File.ReadAllLines
        |> Ok
    with ex -> Error ex.Message

let writeTextToFile (writePath: string) (text: string) : Result<unit, string> =
    try
        (writePath, text)
        |> File.WriteAllText
        |> Ok
    with ex -> Error ex.Message

let writeLinesToFile (writePath: string) (lines: string array) : Result<unit, string> =
    try
        (writePath, lines)
        |> File.WriteAllLines
        |> Ok
    with ex -> Error ex.Message

let copyToBackupFile (tagLibrary: FileInfo) : Result<FileInfo, string> =
    if not tagLibrary.Exists then
        Error "Source tag library file does not exist, so it cannot be backed up."
    else
        let generateBackUpFilePath () : string =
            let baseName = Path.GetFileNameWithoutExtension tagLibrary.Name
            let nowText = DateTimeOffset.Now.ToString "yyyyMMdd_HHmmss"
            let extension = tagLibrary.Extension // Includes the initial period.
            let fileName = $"%s{baseName}.%s{nowText}_backup%s{extension}"
            Path.Combine(tagLibrary.DirectoryName, fileName)

        try
            generateBackUpFilePath()
            |> tagLibrary.CopyTo
            |> Ok
        with ex -> Error ex.Message
