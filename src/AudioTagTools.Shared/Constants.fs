[<AutoOpen>]
module Shared.Constants

open CCFSharpUtils.Library
open System.IO

let timeStampFormat = "yyyyMMdd_HHmmss"

let backUpFile : FileInfo -> Result<FileInfo,string> =
    File.backUpWithTimestamp timeStampFormat
