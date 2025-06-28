<task name="Compare TUI Implementations">

<task_objective>
Compare an existing, robust terminal application named 'Gemini-CLI' built with typescipt to a comparable, prototype C#, .NET 9.0 console application named MaxBot. Both projects are licensed under Open Source Licenses. The C# application, MaxBot, is attempting to implement the same Teriminal User Interface (TUI) experience as the Gemini-CLI but being idomatic to C# and .NET. The comparison I am looking to draw out is how close in behavior, functionality and look and feel is the MaxBot implementations compared to the Gemini CLI.
</task_objective>

<detailed_sequence_steps>
# Compare TUI Implementations Process - Detailed Sequence of Steps

## 1. Understand Gemini-CLI TUI Implementation

1.  Analyze the source code in `tmp/gemini-cli/packages/cli` to understand the TUI implementation.
    - Use `execute_command` with `find tmp/gemini-cli/packages/cli -type f -name "*.tsx" -o -name "*.ts"` to quickly locate UI-related files.
2.  Extensively read the contents of the --all-- the files to build a DETAILED mental model of the application's architecture and data flow. Start with `gemini.tsx` and `ui/App.tsx`. Do NOT skim or assume anything.
3.  Extensively simulate the flow of user input to understand the dynamics of the UI. For example, trace the events that occur when a user types "read the file foo.txt" and presses Enter.
    - NOTE: You must gain understanding of the design dynamics as well as how the terminal gets updated. Be able to answer questions like what happens on the users screen as the user and the ai assistant have a chat and how are the ai tools calls accounted for in the UI
4.  Analyze the visual presentation of the UI components. For example, describe the borders, padding, and margins of the `InputPrompt` and `ToolGroupMessage` components.
    - NOTE: I need to better know that you understand how the display is looking and operating as the user and ai assistant are chatting. for example, the user prompt ui has a border display with the text ...., while the api request is occuring the XYZ component is pushed on the terminal and displays an animiation with the text...
5.  Create Mermaid diagrams to illustrate the component relationships and the user input flow.
6.  Document the findings in a `<gemini-understanding>` tag and present it to the user for feedback (DO NOT WRITE OUT TO A FILE). For example:
    ```
    <gemini-understanding>
      ...
      **User Input Flow and UI Dynamics (Detaild and Extensive):**
      ...
      **Component Diagram (Detaild and Extensive):**
      ...
      **Sequence Diagram User Input Flow (Detailed and Extensive):**
      ...
    </gemini-understanding>
    ```

## 2. Understand MaxBot TUI Implementation

1.  Analyze the source code in `src/UI` to understand the MaxBot TUI implementation.
    - Use `execute_command` with `find src/UI -type f -name "*.cs" -o -name "*.csproj"` to quickly locate UI-related files.
2.  Read the contents of the key files to build a mental model of the application's architecture and data flow. Start with `Program.cs`, `Components/AppComponent.cs`, and `Core/TuiApp.cs`.
3.  Analyze the visual presentation of the UI components, including borders, padding, and margins.
4.  Create Mermaid diagrams to illustrate the component relationships and the user input flow.
5.  Document the findings in a `<maxbot-understanding>` tag and present it to the user for feedback (DO NOT WRITE OUT TO A FILE).

## 3. Identify Similarities

1.  Compare the findings from the previous steps.
2.  Identify and document similarities in functionality in a `<similar>` tag and present it to the user for feedback (DO NOT WRITE OUT TO A FILE).

## 4. Identify Dissimilarities

1.  Compare the findings from the previous steps.
2.  Identify and document where the MaxBot implementation is not similar or incorrect in a `<dissimilar>` tag and present it to the user for feedback (DO NOT WRITE OUT TO A FILE).

## 5. Provide Recommendations

1.  Review the `docs/llmctx/Spectre.Console.md` documentation.
2.  Based on the analysis, provide specific recommendations for improving the MaxBot UI layout and management.
3.  Place the recommendations in a `<recommendation>` tag and present it to the user for feedback (DO NOT WRITE OUT TO A FILE).

## 6. Generate Report

1.  Combine the findings from all previous steps into a single markdown report in the directory `outputs/tui_arch_impl_comparisons/{{TIMESTAMP}}.md`.
2.  Use the `attempt_completion` tool to present the final report.

</detailed_sequence_steps>

</task>
