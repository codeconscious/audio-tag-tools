module Cacher.IO

open Errors
open Shared
open Shared.TagLibrary
open System.IO

type FileTags = TagLib.File

let readfile filePath : Result<string, Error> =
    match IO.readAllText filePath with
    | Ok x -> Ok x
    | Error msg -> Error (ReadFileError msg)

let getFileInfos (dirPath: DirectoryInfo) : Result<FileInfo seq, Error> =
    let isSupportedAudioFile (fileInfo: FileInfo) =
        // Supported file format extensions from https://github.com/mono/taglib-sharp,
        // plus some additional ones. Initial periods are needed.
        [ ".aa"; ".aax"; ".aac"; ".aiff"; ".ape"; ".dsf"; ".flac"; ".m4a"; ".m4b"; "m4p"
          ".mp3"; ".mpc"; ".mpp"; ".ogg"; ".oga"; ".wav"; ".wma"; ".wv"; ".webm"
          ".mp4"; ".opus" ] // This line contains additional custom ones.
        |> List.contains (fileInfo.Extension.ToLowerInvariant())

    try
        dirPath.EnumerateFiles("*", SearchOption.AllDirectories)
        |> Seq.filter isSupportedAudioFile
        |> fun files ->
            if Seq.isEmpty files
            then Error (NoFilesFound dirPath.FullName)
            else Ok files
    with
    | e -> Error (GeneralIoError e.Message)

let parseJsonToTags (json: string) : Result<MultipleLibraryTags, Error> =
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
