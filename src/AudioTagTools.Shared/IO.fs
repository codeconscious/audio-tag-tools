module AudioTagTools.Shared.IO

open System
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, string> =
    try
        fileInfo.FullName
        |> File.ReadAllText
        |> Ok
    with ex -> Error ex.Message

let writeTextToFile (filePath: string) (text: string) : Result<unit, string> =
    try Ok (File.WriteAllText(filePath, text))
    with ex -> Error ex.Message

let writeLinesToFile (filePath: string) (lines: string array) : Result<unit, string> =
    try Ok (File.WriteAllLines(filePath, lines))
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
        with
        | ex -> Error ex.Message
