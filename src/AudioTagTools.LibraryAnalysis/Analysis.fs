module LibraryAnalysis.Analysis

open Shared.TagLibrary
open Shared.Types
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
open FSharpPlus.Data
open FSharpPlus.Operators
open System.IO

module NList = NonEmptyList

type TableRowData = string list nlist option
type RatioData = { Count: int; Total: int; DecimalPlaces: int }

let inline private mostPopulous (count: int) (grouper: 'a -> 'a) (items: 'a nlist) : NonEmptyList<'a * int> =
    items
    |> NList.groupBy grouper
    |> NList.map (fun (_, group) -> (group[0], group.Length))
    |> NList.sortByDescending snd
    |> NList.truncate count

let private asPercentage ratioData : string =
    float ratioData.Count / float ratioData.Total
    |> String.formatPercent ratioData.DecimalPlaces

let filteredArtists tags : string nlist option =
    tags
    |> NList.map (fun tags' ->
        tags'
        |> allDistinctArtists
        |> List.map (fun (Artist artistName) -> artistName)
        |> List.except ignorableAlbumArtistNames)
    |> List.concat
    |> function [] -> None | artists -> Some (NList.ofList artists)

let uniqueArtistCount tags : int =
    tags
    |> filteredArtists
    |> Option.map (NList.distinct >> NList.length)
    |> Option.defaultValue 0

let averageFileSize tags : int64 =
    let sizeTotal = tags |> NList.map _.FileSize |> NList.sum
    let fileCount = tags.Length
    sizeTotal / (int64 fileCount)

let albumArtPercentage tags : string =
    tags
    |> NList.choose (fun t -> Option.isPos t.ImageCount)
    |> NList.length
    |> fun count -> asPercentage { Count = count; Total = tags.Length; DecimalPlaces = 2 }

let topArtists count tags : TableRowData =
    filteredArtists tags
    |> Option.map (fun artists ->
        let artistCount = artists.Length

        artists
        |> mostPopulous count id
        |> NList.map (fun (artist, count) ->
            [ artist
              String.formatInt count
              asPercentage { Count = count; Total = artistCount; DecimalPlaces = 3 } ]) )

let topAlbums count tags : TableRowData =
    let albums = tags |> NList.map _.Album
    let albumCount = albums.Length

    albums
    |> mostPopulous count id
    |> NList.map (fun (album, count) ->
        [ album
          String.formatInt count
          asPercentage { Count = count; Total = albumCount; DecimalPlaces = 2 } ])
    |> Some

let topTitles count tags : TableRowData =
    tags
    |> NList.map _.Title
    |> mostPopulous count String.toLower
    |> NList.map (fun (title, count) -> [ title; String.formatInt count ])
    |> Some

let topGenres count tags : TableRowData =
    tags
    |> NList.tryFilter (fun t -> Array.isNotEmpty t.Genres)
    |> Option.map (fun t ->
        let genres = t >>= fun xs -> xs.Genres |> NList.ofArray

        genres
        |> mostPopulous count String.toLower
        |> NList.map (fun (genre, count) ->
            [ genre
              String.formatInt count
              asPercentage { Count = count; Total = genres.Length; DecimalPlaces = 2 } ]))

let artistsWithMostGenres count tags : TableRowData =
    let genreCounts (genres: string list) : string =
        genres
        |> List.countBy id
        |> List.sortBy fst
        |> List.map (fun (g, count) -> $"{g} ({count})")
        |> String.concat "; "

    let extractArtistGenreInfo (a, tags) =
        let genres = tags |> NList.map _.Genres |> Array.concat |> Array.map _.Trim() |> List.ofArray
        let uniqGenreCount genres = genres |> List.distinctIgnoreCase |> _.Length
        (a, uniqGenreCount genres, genres)

    tags
    |> NList.tryFilter hasAnyArtist
    |> Option.map (NList.groupBy firstDistinctArtist
                >> NList.map extractArtistGenreInfo
                >> NList.sortByDescending item2
                >> NList.take count
                >> NList.map (fun (Artist artist, uniqGenreCount, genres) ->
                    [ artist
                      String.formatInt uniqGenreCount
                      genreCounts genres ]))

let largestFiles count tags : TableRowData =
    tags
    |> NList.sortByDescending _.FileSize
    |> NList.truncate count
    |> NList.map (fun file ->
        let artist = String.concat ", " file.Artists
        [ $"{artist} / {file.Title}"
          String.formatBytes file.FileSize ])
    |> Some

let uppercaseFileExtension tagFile =
    ((Path.GetExtension tagFile.FileName)[1..]).ToUpperInvariant()

let topFormats count tags : TableRowData =
    tags
    |> NList.map uppercaseFileExtension
    |> mostPopulous count id
    |> NList.map (fun (ext, count) -> [ $"{ext}"; String.formatInt count ])
    |> Some

let topBitRates count tags : TableRowData =
    tags
    |> NList.map _.BitRate
    |> mostPopulous count id
    |> NList.map (fun (bitrate, count) ->
        [ $"{bitrate} kbps"
          String.formatInt count ])
    |> Some

let topSampleRates count tags : TableRowData =
    tags
    |> NList.map _.SampleRate
    |> mostPopulous count id
    |> NList.map (fun (sampleRate, count) ->
        [ $"{String.formatInt sampleRate}"
          String.formatInt count ])
    |> Some

let topQualityData count tags : TableRowData =
    tags
    |> NList.map (fun t ->
        {| BitRate    = t.BitRate
           SampleRate = t.SampleRate
           Extension  = uppercaseFileExtension t |})
    |> mostPopulous count id
    |> NList.map (fun (data, count) ->
        [ data.Extension
          $"{data.BitRate} kbps"
          String.formatInt data.SampleRate
          String.formatInt count ])
    |> Some

let longestFileNames count tags : TableRowData =
    tags
    |> NList.map (fun t -> t.FileName.Length, t)
    |> NList.sortByDescending fst
    |> NList.take count
    |> NList.map (fun (count, t) ->
        [ $"""{mainArtists "; " t}{String.nl}↪︎ {t.Title}"""
          t.FileName
          String.formatInt count ])
    |> Some
