module Shared.IO

open CCFSharpUtils.Library
open System
open System.IO
open FSharpPlus.Data

let readFile (fileInfo: FileInfo) : Result<string, string> =
    try
        fileInfo.FullName
        |> File.ReadAllText
        |> Ok
    with ex -> Error ex.Message

/// Reads all text from the specified file, returning a Result.
/// If an exception is thrown during the underlying operation,
/// the Error includes the exception itself.
let readAllText (filePath: string) : Result<string, string> =
    ofTry (fun _ -> File.ReadAllText filePath)

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

/// If the file exists, returns its FileInfo wrapped in Ok. Otherwise, returns an error list.
/// Intended to be used in applicative validation chains. (Thus, the list.)
let validateToFileInfo (err: 'err) (file: string) : Result<FileInfo, 'err list> =
    if File.Exists file
    then Ok (FileInfo file)
    else Error [err]

/// If the directory exists, returns its DirectoryInfo wrapped in Success.
/// Otherwise, returns the error wrapped in Failure.
/// Intended to be used in applicative validation chains.
let validateToDirInfo (err: 'err) (dir: string) : Validation<'err, DirectoryInfo> =
    if Directory.Exists dir
    then Success (DirectoryInfo dir)
    else Failure err
