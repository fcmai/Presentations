﻿#I @"../packages"
#r @"Accord.2.15.0\lib\net45\Accord.dll"
#r @"Accord.Math.2.15.0\lib\net45\Accord.Math.dll"
#r @"Accord.Statistics.2.15.0\lib\net45\Accord.Statistics.dll"
#r @"Accord.MachineLearning.2.15.0\lib\net45\Accord.MachineLearning.dll"

open System
open System.IO
 
open Accord.MachineLearning
open Accord.MachineLearning.VectorMachines
open Accord.MachineLearning.VectorMachines.Learning
open Accord.Statistics.Kernels

#load "Visualizer.fs"
open Digits.Visualizer
 
(* 
The dataset I am using here is a subset of the Kaggle digit recognizer;
download it first on your machine, and correct path accordingly.
Training set of 5,000 examples: 
http://brandewinder.blob.core.windows.net/public/trainingsample.csv
Validation set of 500 examples, to test your model:
http://brandewinder.blob.core.windows.net/public/validationsample.csv
*)

let root = __SOURCE_DIRECTORY__ 
let training = root + "/trainingsample.csv"
let validation = root + "/validationsample.csv"
 
let readData filePath =
    File.ReadAllLines filePath
    |> fun lines -> lines.[1..]
    |> Array.map (fun line -> line.Split(','))
    |> Array.map (fun line -> 
        (line.[0] |> Convert.ToInt32), (line.[1..] |> Array.map Convert.ToDouble))
    |> Array.unzip
 
let labels, observations = readData training

// Let's visualize a couple of the data points
for i in 0 .. 9 do 
    Digits.Visualizer.draw (labels.[i], observations.[i]) (string labels.[i])

(*
Note that while this is a small dataset, loading the data
is an expensive part of the process. 
*)

let features = 28 * 28
let classes = 10
 
let algorithm = 
    fun (svm: KernelSupportVectorMachine) 
        (classInputs: float[][]) 
        (classOutputs: int[]) (i: int) (j: int) -> 
        let strategy = SequentialMinimalOptimization(svm, classInputs, classOutputs)
        strategy :> ISupportVectorMachineLearning
 
let kernel = Linear()
let svm = new MulticlassSupportVectorMachine(features, kernel, classes)
let learner = MulticlassSupportVectorLearning(svm, observations, labels)
let config = SupportVectorMachineLearningConfigurationFunction(algorithm)
learner.Algorithm <- config
 
let error = learner.Run()
 
printfn "Error: %f" error

(*
Are we done yet? Not quite.
The proof of the model is in how it deals with data 
it has never seen before, hence the validation set.
*)
  
let validationLabels, validationObservations = readData validation
 
let correct =
    Array.zip validationLabels validationObservations 
    |> Array.map (fun (l, o) -> if l = svm.Compute(o) then 1. else 0.)
    |> Array.average
 
let view =
    let rng = Random()
    let size = Array.length validationLabels
    seq { for i in 0 .. 9 do 
              let x = rng.Next(size)
              let (real,pixels) = validationLabels.[x], validationObservations.[x]
              let pred = svm.Compute(pixels)
              yield (real,pixels),pred
        }
        |> Seq.iter (fun ((real,pixels),pred) -> 
            Digits.Visualizer.draw (real,pixels) (sprintf "%i -> %i" real pred))

(*
At that point in time we have a decent prediction model.
Now we can work on making it better - that is, either
faster or more accurate (or both!). The beauty of having
a REPL here is that I don't need to reload data, 
I can just keep going. 
*)