// ------------------------------------------------------------------------------------------------
// Transformations - a list of 'Transformation' values represents a SQL-like query
// ------------------------------------------------------------------------------------------------

type GroupAggregation = 
  | GroupKey
  | CountAll
  | CountDistinct of string
  | ConcatValues of string
  | Sum of string
  | Mean of string

type SortDirection =
  | Ascending
  | Descending 

type Paging =
  | Take of string
  | Skip of string
  
type FilterOperator = 
  | And | Or

type RelationalOperator = 
  | Equals 
  | NotEquals 
  | LessThan
  | GreaterThan 
  | InRange
  | Like

type FilterCondition = RelationalOperator * string * string

type Transformation = 
  | DropColumns of string list
  | SortBy of (string * SortDirection) list
  | GroupBy of string list * GroupAggregation list
  | FilterBy of FilterOperator * FilterCondition list
  | Paging of Paging list
  | Empty

// ------------------------------------------------------------------------------------------------
// Primitive types 
// ------------------------------------------------------------------------------------------------

type PrimitiveType = 
  | Bool 
  | Date 
  | Number 
  | String 

let isNumeric = function Number -> true | _ -> false
let isConcatenable = function String -> true | _ -> false
let isBool = function Bool -> true | _ -> false
let isDate = function Date -> true | _ -> false

// ------------------------------------------------------------------------------------------------
// Fields & how they are transformed by operations
// ------------------------------------------------------------------------------------------------

type Field = 
  { Name : string 
    Type : PrimitiveType }

// If we have a file with given fields and perform a specific transformation,
// this function tells us what fields are going to be in the returned data set.
// (The only tricky thing is 'grouping' where the new fields depend on the
// aggregations that we specified.)
let singleTransformFields fields = function
  | Empty -> fields
  | SortBy _ -> fields
  | Paging _ -> fields
  | FilterBy _ -> fields
  | DropColumns(drop) ->
      let dropped = set drop
      fields |> List.filter (fun f -> not(dropped.Contains f.Name))
  | GroupBy(flds, aggs) ->
      let oldFields = dict [ for f in fields -> f.Name, f ]
      aggs 
      |> List.collect (function
          | GroupAggregation.GroupKey -> List.map (fun f -> oldFields.[f]) flds
          | GroupAggregation.ConcatValues fld
          | GroupAggregation.Sum fld -> [ oldFields.[fld] ]
          | GroupAggregation.Mean fld -> [ oldFields.[fld] ]
          | GroupAggregation.CountAll -> [ { Name = "count"; Type = PrimitiveType.Number } ]
          | GroupAggregation.CountDistinct fld -> [ { Name = oldFields.[fld].Name; Type = PrimitiveType.Number } ])
      
let transformFields fields tfs = 
  tfs |> List.fold singleTransformFields (List.ofSeq fields) |> List.ofSeq

// ------------------------------------------------------------------------------------------------
// Representation of generated types
// ------------------------------------------------------------------------------------------------

type ProvidedType = 
  | Delayed of (unit -> ProvidedType)
  | Method of (string * ProvidedType) list * ProvidedType
  | Object of Member list 
  | Primitive of PrimitiveType

and Member = 
  { Name : string 
    Transformations : Transformation list
    Type : ProvidedType }

type Context = 
  { InputFields : Field list
    Fields : Field list }

// ------------------------------------------------------------------------------------------------
// The main logic to generate types based on 'Context'
// ------------------------------------------------------------------------------------------------

let makeObjectType members = 
  Object members
  
let rec makeProperty ctx name tfs = 
  { Member.Name = name; Type = makePivotType ctx tfs; Transformations = tfs }
  
and makeMethod ctx name tfs callid args = 
  { Member.Name = name
    Type = Method([ for n, t in args -> n, Primitive t ], makePivotType ctx tfs)
    Transformations = tfs }

and handlePagingRequest ctx rest pgid ops =
  let takeMemb = 
    makeMethod ctx "take" (Empty::Paging(List.rev (Take(pgid + "-take")::ops))::rest) (pgid + "-take") ["count", PrimitiveType.Number] 
  let skipMemb = 
    makeMethod ctx "skip" (Paging(Skip(pgid + "-skip")::ops)::rest) (pgid + "-skip") ["count", PrimitiveType.Number] 
  let thenMemb = 
    makeProperty ctx "then" (Empty::Paging(List.rev ops)::rest)
  ( match ops with
    | [] -> [skipMemb; takeMemb; thenMemb]
    | [Skip _] -> [takeMemb; thenMemb]
    | _ -> failwith "handlePagingRequest: Shold not happen" ) |> makeObjectType

and handleDropRequest ctx rest dropped = 
  let droppedFields = set dropped
  [ yield makeProperty ctx "then" (Empty::DropColumns(dropped)::rest)
    for field in ctx.Fields do
      if not (droppedFields.Contains field.Name) then
        yield 
          makeProperty ctx ("drop " + field.Name) (DropColumns(field.Name::dropped)::rest) ]
  |> makeObjectType    

and handleSortRequest ctx rest keys = 
  let usedKeys = set (List.map fst keys)
  [ yield makeProperty ctx "then" (Empty::SortBy(keys)::rest)
    for field in ctx.Fields do
      if not (usedKeys.Contains field.Name) then
        let doc = sprintf "Use the field '%s' as the next sorting keys" field.Name
        let prefix = if keys = [] then "by " else "and by "
        yield makeProperty ctx (prefix + field.Name) (SortBy((field.Name, Ascending)::keys)::rest) 
        yield makeProperty ctx (prefix + field.Name + " descending") (SortBy((field.Name, Descending)::keys)::rest) ]
  |> makeObjectType    

and aggregationMembers ctx rest keys aggs = 
  let containsCountAll = aggs |> Seq.exists ((=) GroupAggregation.CountAll)
  let containsField fld = aggs |> Seq.exists (function 
    | GroupAggregation.CountDistinct f | GroupAggregation.ConcatValues f 
    | GroupAggregation.Sum f | GroupAggregation.Mean f -> f = fld 
    | GroupAggregation.CountAll | GroupAggregation.GroupKey -> false)

  let makeAggMember name agg = 
    makeProperty ctx name (GroupBy(keys,aggs @ [agg])::rest) 

  [ yield makeProperty ctx "then" (Empty::GroupBy(keys, aggs)::rest) 
    if not containsCountAll then 
      yield makeAggMember "count all" GroupAggregation.CountAll
    for fld in ctx.Fields do
      if not (containsField fld.Name) then
        yield makeAggMember ("count distinct " + fld.Name) (GroupAggregation.CountDistinct fld.Name) 
        if isConcatenable fld.Type then
          yield makeAggMember ("concatenate values of " + fld.Name) (GroupAggregation.ConcatValues fld.Name)
        if isNumeric fld.Type || isBool fld.Type then
          yield makeAggMember ("average " + fld.Name) (GroupAggregation.Mean fld.Name)
          yield makeAggMember ("sum " + fld.Name) (GroupAggregation.Sum fld.Name) ]

and handleGroupAggRequest ctx rest keys aggs =
  aggregationMembers ctx rest keys aggs  
  |> makeObjectType  
  
and handleGroupRequest ctx rest keys = 
  let prefix = if List.isEmpty keys then "by " else "and "
  [ for field in ctx.Fields ->
      makeProperty ctx (prefix + field.Name) (GroupBy(field.Name::keys, [])::rest) 
    if not (List.isEmpty keys) then
      yield! aggregationMembers ctx rest keys [GroupAggregation.GroupKey] ]
  |> makeObjectType  

and handleFilterEqNeqRequest ctx rest (fld, eq) op conds = 
  let tfs = 
    if op = Or then rest 
    elif List.isEmpty conds then rest 
    else FilterBy(op, conds)::rest
  let tfs = 
    tfs |> List.filter (function 
      | FilterBy(_, conds) when conds |> List.exists (function ((Equals | NotEquals), _, _) -> false | _ -> true) -> false
      | _ -> true)
  
  printfn "%A" tfs 
  let options = [| "One"; "Two"; "Three" |]
  [ for opt in options do
      yield makeProperty ctx opt (FilterBy(op, (eq, fld, opt)::conds)::rest) ] 
  |> makeObjectType 

and handleFilterRequest ctx rest flid op conds = 
  let prefixes = 
    match conds, op with
    | [], _ -> ["", And] 
    | _::[], _ -> ["and ", And; "or ", Or]
    | _, And -> ["and ", And] 
    | _, Or -> ["or ", Or]
  [ for prefix, op in prefixes do
      for field in ctx.Fields do
        if field.Type = PrimitiveType.String then
          yield makeProperty ctx (prefix + field.Name + " is") (FilterBy(op, (Equals, field.Name, "!")::conds)::rest) 
          yield makeProperty ctx (prefix + field.Name + " is not") (FilterBy(op, (NotEquals, field.Name, "!")::conds)::rest) 
          yield makeMethod ctx (prefix + field.Name + " contains") (FilterBy(op, (Like, field.Name, flid)::conds)::rest) flid ["text", PrimitiveType.String]
        if field.Type = PrimitiveType.Number then
          yield makeMethod ctx (prefix + field.Name + " is less than") (FilterBy(op, (LessThan, field.Name, flid)::conds)::rest) flid ["value", PrimitiveType.Number]
          yield makeMethod ctx (prefix + field.Name + " is greater than") (FilterBy(op, (GreaterThan, field.Name, flid)::conds)::rest) flid ["value", PrimitiveType.Number]
          yield makeMethod ctx (prefix + field.Name + " is in range") (FilterBy(op, (InRange, field.Name, flid)::conds)::rest) flid ["minimum", PrimitiveType.Number; "maximum", PrimitiveType.Number]
        if field.Type = PrimitiveType.Date then
          yield makeMethod ctx (prefix + field.Name + " is less than") (FilterBy(op, (LessThan, field.Name, flid)::conds)::rest) flid ["value", PrimitiveType.Date]
          yield makeMethod ctx (prefix + field.Name + " is greater than") (FilterBy(op, (GreaterThan, field.Name, flid)::conds)::rest) flid ["value", PrimitiveType.Date]
          yield makeMethod ctx (prefix + field.Name + " is in range") (FilterBy(op, (InRange, field.Name, flid)::conds)::rest) flid ["minimum", PrimitiveType.Date; "maximum", PrimitiveType.Date]
    if not (List.isEmpty conds) then
      yield makeProperty ctx "then" (Empty::FilterBy(op, conds)::rest) ]
  |> makeObjectType  

and makePivotType ctx tfs = Delayed(fun () -> 
  let last, rest = match tfs with last::rest -> last, rest | _ -> Empty, []
  let ctx = { ctx with Fields = transformFields ctx.InputFields (List.rev rest) }
  match last with
  | Empty ->
      [ yield makeProperty ctx "group data" (GroupBy([], [])::rest) 
        yield makeProperty ctx "filter data" (FilterBy(And, [])::rest) 
        yield makeProperty ctx "sort data" (SortBy([])::rest) 
        yield makeProperty ctx "drop columns" (DropColumns([])::rest) 
        yield makeProperty ctx "paging" (Paging([])::rest) ]
      |> makeObjectType    
  | Paging(ops) ->
      let pgid = rest |> Seq.sumBy (function Paging _ -> 1 | _ -> 0) |> sprintf "pgid-%d"  
      handlePagingRequest ctx rest pgid ops
  | SortBy(keys) ->
      handleSortRequest ctx rest keys
  | DropColumns(dropped) ->
      handleDropRequest ctx rest dropped
  | FilterBy(fop, (rop & (Equals | NotEquals), fld, "!")::conds) ->
      handleFilterEqNeqRequest ctx rest (fld, rop) fop conds
  | FilterBy(op, conds) ->
      let flid = conds.Length + (Seq.sumBy (function FilterBy(_, cds) -> cds.Length | _ -> 0) rest)
      handleFilterRequest ctx rest (sprintf "flid-%d" flid) op conds
  | GroupBy(flds, []) ->
      handleGroupRequest ctx rest flds
  | GroupBy(flds, aggs) ->
      handleGroupAggRequest ctx rest flds aggs )

// ------------------------------------------------------------------------------------------------
// Small interactive demo to navigate through a type
// ------------------------------------------------------------------------------------------------

let rec exploreType provided = 
  match provided with 
  | Delayed f -> exploreType (f ())
  | Object members -> 
      printfn "OBJECT TYPE WITH MEMBERS"
      for i, m in Seq.indexed members do
        printfn $" ({i}) {m.Name}"
      printf "Choose a member: "
      let i = int (System.Console.ReadLine())
      let selected = members.[i]
      printfn "SELECTED TRANSFORMATIONS"
      printfn $"  {selected.Transformations}"
      exploreType selected.Type
  | Method _ ->
      printfn "UNEXPECTED METHOD TYPE"
  | Primitive _ ->
      printfn "UNEXPECTED PRIMITIVE TYPE"


let flds = 
  [ { Field.Name = "Name"; Type = String }
    { Field.Name = "Age"; Type = Number } ]

let ctx = { Fields = flds; InputFields = flds }
let provided = makePivotType ctx []

exploreType provided