module IO

open System.IO
open Errors
open Utilities
open TagLibrary

type FileTags = TagLib.File

let readfile filePath : Result<string, Error> =
    match readAllText filePath with
    | Ok x -> Ok x
    | Error msg -> Error (ReadFileError msg.Message)

let getFileInfos (dirPath: DirectoryInfo) : Result<FileInfo seq, Error> =
    let isSupportedAudioFile (fileInfo: FileInfo) =
        // Supported formats from https://github.com/mono/taglib-sharp.
        ["aa"; "aax"; "aac"; "aiff"; "ape"; "dsf"; "flac"; "m4a"; "m4b"; "m4p"
         "mp3"; "mpc"; "mpp"; "ogg"; "oga"; "wav"; "wma"; "wv"; "webm"]
        |> List.contains fileInfo.Extension

    try
        dirPath.EnumerateFiles("*", SearchOption.AllDirectories)
        |> Seq.filter isSupportedAudioFile
        |> Ok
    with
    | e -> Error (GeneralIoError e.Message)

let parseJsonToTags (json: string) : Result<LibraryTags array, Error> =
    parseJsonToTags json
    |> Result.mapError LibraryTagParseError

let parseFileTags (filePath: string) : Result<FileTags option, Error> =
    try
        FileTags.Create filePath
        |> Option.ofObj
        |> Ok
    with e -> Error (FileTagParseError e.Message)

let writeFile (filePath: string) (content: string) : Result<unit, Error> =
    try Ok (File.WriteAllText(filePath, content))
    with e -> Error (WriteFileError e.Message)
