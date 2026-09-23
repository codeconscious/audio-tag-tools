[<AutoOpen>]
module Shared.Types

open System

[<NoComparison>]
type Json = Json of string

[<CustomEquality; CustomComparison>]
type Artist =
    | Artist of string

    /// Performs a case-insensitive comparison.
    override this.Equals obj =
        match obj with
        | :? Artist as (Artist a2) ->
            let (Artist a1) = this
            String.Equals(a1, a2, StringComparison.InvariantCultureIgnoreCase)
        | _ -> false

    override this.GetHashCode () =
        let (Artist s) = this
        s.ToLowerInvariant().GetHashCode()

    interface IComparable<Artist> with
        /// Performs a case-insensitive comparison.
        member this.CompareTo other =
            let (Artist a1) = this
            let (Artist a2) = other
            String.Compare(a1, a2, StringComparison.InvariantCultureIgnoreCase)

    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | :? Artist as other -> (this :> IComparable<Artist>).CompareTo other
            | _ -> invalidArg (nameof obj) "Cannot compare Artist with non-Artist"
