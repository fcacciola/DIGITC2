# Transgraphier 2.4.1 User Manual

Digital Instrumental Trans-Communication (ITC) workbench

# Purpose

Transgraphier is an experimental Instrumental Trans-Communication (ITC) research tool.

In ordinary Electronic Voice Phenomena (EVP) work, an operator records noise or ambient audio and later listens for possible voice-like messages. Transgraphier explores a different and more constrained idea: if an unknown communicating agency can influence a noisy recording at all, perhaps the most robust signal it could imprint is not spoken voice, but a timed sequence of sound-energy bursts representing a text message.

The application therefore tries to extract an English message from an EVP-style recording by looking for burst patterns that can be interpreted as a tap-coded message.

This is a research tool. It does not prove that a message is paranormal, spiritual, or externally generated. Noise, artifacts, expectation, and ordinary signal-processing effects can all produce patterns. The app is designed to make the decoding attempt more objective and inspectable by showing the intermediate processing stages.

# Usage

The first step is to, either **Choose an audio file** from your computer, or **Record** the audio directly in the app.

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{welcome.png}
\caption{Welcome}
\end{figure}

After that, the Waveform of the input file is shown:

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{loaded.png}
\caption{Input Audio loaded}
\end{figure}

The next step is to press **Process**.

The app then processes the audio in an attempt to extract a *TapCode-encoded Text Message* (processing, downloading and displaying the result can take a while, depending on the length of the file).

The processing result is made of several parts:

 - The decoded message (on the top left box).
    \begin{quote}
    If a message was decoded, it is displayed in a \textbf{\textcolor{blue}{bold, blue text}}.
  
    If no message was decoded, is is indicated in a \textcolor{red}{red text}.
  
    The decoded text or the failure message is followed by a \emph{Score}.
  
    The score is followed by a \emph{Branch Name}: processing may eventually branch out in order to try different parameters. The branch with the highest score is the result.
    \end{quote}

 - A sequence of intermediate text results (on the top right box)
   
 - A sequence of *signals* (right below the Waveform of the input)
    \begin{quote}
     There are two types of result signals: \textbf{Audio Signals} and \textbf{Lexical Signals}.
     
     A Lexical Signal contains a sequence of \textbf{Symbols}, such that each symbol is located at a specific point in time along the input sound. For example, sequences of \emph{bits, bytes, letters and words} are all coming out as Lexical Signals.
     
     Both audio and lexical signals are displayed as Waveforms in the order in which they are produced.
     
   Depending on how far the processing went, some or all of the signals will be produced and displayed.
    \end{quote}
   
 - A full detailed text log (at the very bottom).

All of the displayed waveforms are _in sync_. You can place the cursor over _any_ of them and use the mouse to Pan and Zoom _All of them at once_ (centered at the position of the cursor)In or Out.  


Below is a screen capture of an artificial _"successful"_ example.

**NOTE:** The input sound file used to illustrated a positive result is totally artificial. It was produced by an IA agent instructed to create a sound file containing the desired text and encoding.\
Transgraphier then took this file and followed the processing as usual, but the input file itself is artificial.

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{positive.png}
\caption{Message successfully decoded}
\end{figure}


And below an Unsuccessful one:

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{failure.png}
\caption{No message decoded}
\end{figure}


# Processing Details

## Overview

The algorithm does, roughly, the following:

 - Preprocess, detect and isolate **Pulses** within the input audio. A Pulse is any isolated amplitude burst above the noise floor. The duration or shape of the pulse is not important, only its **Onset**: the time at which it ocurrs.
 
 - Compute *Pulse to Pulse* time gaps.
 
 - Find special runs of 5 or more pulses that are *Collectively Consecutive*. That is, their inter-pulse gap is roughly the same for all of them. These runs of pulses are called **Separators**.
 
 - Split the entire sequence of pulses into **Blocks** (based on the separators).
 
 - Within each Block:
 
   - Classify each pulse as **Intra-Count**, or not. This is decided upon the duration of the Gap to the Previous Pulse.
   
   - Group pulses into so-called **Tap Codes** (based on their Intra-Count classification).
   
   - Convert each *Tap Code* into a **Binary Symbol**: `0, 1, or ? (for bits that are uncertain)`.
   
   - Group and convert the sequence of binary symbols into **Scored byte Symbols** (taking uncertain bits into account).
   
   - Convert each scored-byte symbol into a **Scored ASCII Symbols**.
   
 - Each partial result has a Score which is combined into the overall result score displayed on the top-left box. 

## Branching

There are certain steps along the processing pipeline at which alternative paths, with different parameters, might be taken. These are called **Branches**.

At the very end, there will be one or more results, each corresponding to a separate processing branch. 

Each branch gets an overall Score, and the winning branch is the one effectively selected as final result.

## Noise Gate

The first major step is a noise floor gate. This steps calculates the so-called **Noise Floor** and subtracts that from the entire signal.
The result is that only those *Amplitude Peaks* that are *above* the noise floor remain.

Ideally, each remaining peak roughly maps to a single **"Tap"** (for instance, when a text message was nicely encoded as "correctly-timed, isolated energy bursts", as with the artificial example from before). 

The gated signal is shown right below the input signal.


\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{noise.png}
\caption{Noise gate}
\end{figure}

## Envelope And Upward Compression

The gated signal is then smoothed with a so-called **"Envelope Following"** step, and further enhanced with an **"Upward Compression"**.

These steps help equalize levels so that weaker but potentially meaningful bursts can be compared more fairly with stronger ones.

The results of the envelope and upward compression is shown in the waveform right below the noise gate.

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{compressed.png}
\caption{Upward-compressed signal (after Envelope)}
\end{figure}

## Discretization

The signal is then converted into a square wave. This is shown in the **Discretized** waveform.

At this stage, every square matches a single Tap.

The signal from the previous step can be seen as being a sequence of **"Hills and Valleys"**. Discretization converts every "hill" into a square of a fixed, flat height.\
A Valley is the portion of the signal that separates two distinct Hills. That is the case when its amplitude is significantly lower than that of the neighboring hills.

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{discretized.png}
\caption{Discretized signal}
\end{figure}


## Pulse Extraction

Each square-wave whose width (in Seconds) is larger than a given **"Minimum Pulse Width"** is now converted into a **"Pulse"**.

The pulses are shown as square-waves in the Waveform right below Discretized.

Internally, however, the pulses are *NOT* an audio signal. Instead, they are a so-called **"Lexical Signal"**, which is just Rendered as square waves so you can see the pulses (as all the following waveforms showing symbols rendered as squares).

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{pulses.png}
\caption{Pulses}
\end{figure}

## Taps

Every Pulse is now converted into a **"Tap"**. 

Every Tap has a **"Gap"**, which is the distance, in Seconds, to the previous Tap.

From the sequence of Gaps, the processing calculates, via statistical analysis, a specific duration known as the **"Intra-Count Gap"**.

Then, every Tap with a Gap no larger than this value is *classified* as an **"Intra-Count Tap"**.

All Taps are shown, color coded, in the next waveform.

The taps classified as Intra-Count are drawn in **Green**. The rest in **Yellow**.

With all taps classified (Green or Yellow, Intra-Count or not), a **Tap Counting** is calculated in the following way:

Every Yellow Tap *starts a new* formal count of 1. If, for example, there are only yellow taps, then all counts are simply 1.

Every sequence of `N` Green Taps, precedded by a Yellow Tap, corrresponds to a count of `N+1` (+1 because of the yellow that started the count)

For example, the figure below corresponds to the following sequence of Counts:

```1, 1, 1, 2, 1, 1, 2, 1, 1, 1, 2, 1, 1, 1, 6```

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{taps.png}
\caption{Taps}
\end{figure}

## Tap-Codes

The **"Counts"** from the previous step are now grouped as Tap-Codes and Separators:

Every count of 5 or more is a **Separator**.\
Within separators, counts are paired into a so-called **"Tap Code"**.\
Each Tap Code has a **Row Count** and a **Column Count**, in that order.

Tap Codes and Separators are shown, color-coded, in the next Waveform. Row counts are Blue, Column counts are Red, and Separators are black:

For example, the Waveform below shows the following groups of tap codes (the separator is represented by (0,0))

```
(1,1),(1,2),(1,1),(1,2),(1,1),(1,2),(1,1),(1,1),(0,0)
(1,1),(1,2),(1,2),(1,1),(1,2),(1,1),(1,1),(1,1),(0,0)
```

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{tapcodes.png}
\caption{Tap Codes}
\end{figure}

## Text Decoding and Scoring

Each tap code is a pair of counts that corresponds to the *Row* and *Column* indices into a special matrix known as a **"Polybius Square"**. The square has *Symbols* into each possible cell.

Here is a simple _binary_ Polybius Square:

\begin{figure}[H]
\centering
\includegraphics{binary_square.png}
\caption{Binary Polybius Square}
\end{figure}


As you can see, using the simple binary square, the tap-code `(1,1)` corresponds to the very first Cell, at the top-left, and it contains the symbol `0`. Likewise, `(1,2)` maps to `1`, while `(2,2)` maps to `?`.

Therefore, each extracted tap code, if any, is converted into a **Uncertain Binary Digit**: `0, 1 or ?`

Each tap-code *group* corresponds to a so-called **"Fuzzy Byte"** such as `**010?01??**`. This a byte in which certain bits are unknown.  Every fuzzy byte is converted into a **"Scored Resolved Byte"**. That is well-formed byte, in which every '?' is resolved as a '0', *but*, the bye is given a *Score* based on the ratio of certain vs uncertain bits.  For example, the fuzzy-byte: `**00000000**` maps to a value of 0 with a score of 1 (100%), but `**????????**` maps to a value which is also 0 but with a score of 0.

Each resolved byte is then mapped into an ASCII letter, if any, or into the letter "!". Separator letters (such as a Space) are found, and the entire letters stream split into words. Each word is looked up into an English dictionary and given a score.

Each individual score along the process is combined into a unified overall score.

## Blocks

Depending on the successful extraction of separators, the entire audio is split up into Blocks. Within each block, the tap-codes are converted into Uncertain Binary Digits and these grouped into a Fuzzy Byte, which is then mapped into a Resolved Byte.

Since it is the separators which mark the block boundaries, it can very well happen that a given block has less or more than 8 Uncertain Bits.
If it has less than 8, it is just paired. If it has more, the ones with the highest score, in their order, are kept, and any additional excess is trimmed.

For example, the fuzzy-byte: `**0101**` maps into the actual byte `**01010000**`. The fuzzy-byte: `**0?1?0?0?1?1????01**` maps to `**01001101**` because the certain bits win over the uncertain ones in this case of excess. Likewise, the fuzzy-byte `**10100101111111**` becomes just `**10100101**` (the trailing ones are just trimmed).

If separators where found, so it was possible to extract blocks, the *Block Navigation Buttons*, at the very top, will be enabled. You can then *Zoom* into *One* block at a time, and move forward or backwards across all detected blocks:

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{block.png}
\caption{Blocks}
\end{figure}


## Text Timeline

If the processing was able to extract bytes, then each block corresponds to a byte, and each byte to an ASCIII **Letter** (or a **!** if the byte does not match any ASCII). 

The letters are shown in the **Timeline** view, at bottom. Each letter is aligned with its block.

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{timeline.png}
\caption{Text Timeline}
\end{figure}

## Parameters ad Re-processing

Right below the Input File waveform, there is a table with some processing parameters:

\begin{figure}[H]
\centering
\includegraphics[width=\linewidth]{params.png}
\caption{Parameters}
\end{figure}

These are calculated during processing and displayed along with the results.

If no text was decoded, or is too noisy, you can change any of these and click on Process another time. The modified parameters will be used instead of getting automatically calculated as it was the first time.

  - Noise floor (`0.0 to 1.0` value)

    If the signal has too much noise, the automated noise floor calculation can get it wrong.\
    You might see that by inspecting the Noise Gate waveform in the result: if there aren't lots of \textbf{isolated} pulses, the noise floor might need to to be higher. Counterwise, if there are too few pulses, then noise floor might need to be lowered.
    
  - Discretize Merge Prominence (`0.0 to 1.0` value)

    Discretization is based on measuring the \emph{amplitude variations} to detect Hill->Valley->Hill sequences. It has to decide whether two initially separate hills are actually one single "merged" hill. The decision is controlled by this parameter. 
    If a text message is somewhat correctly decoded but some or several letters seem wrong, or separator missing, you can closely inspect the correlation between the upward-compress signal and the discretized square-wave (zooming and panning along the signals). If you spot pulses that are either separated when they should be merged, or the opposite, you can fine tune this parameter and try again in order to get a cleaner text result.
    
  - Mimimun Pulse Width (seconds)

  - Intra-Count gap (seconds)

