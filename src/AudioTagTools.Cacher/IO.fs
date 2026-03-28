module Cacher.IO

open Errors
open FSharpPlus.Data
open CCFSharpUtils.Library
open System.IO

type FileTags = TagLib.File

let getFileInfos (dirPath: DirectoryInfo) : Result<FileInfo NonEmptySeq, CacherError> =
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
        |> Seq.toNonEmptySeqResult (NoFilesFound dirPath.FullName)
    with
    | e -> Error (GeneralIoError e.Message)
