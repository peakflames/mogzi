<task name="Compare TUI Implementations">

<task_objective>
Compare an existing, robust terminal application named 'Gemini-CLI' built with typescipt to a comparable, prototype C#, .NET 9.0 console application named MaxBot. Both projects are licensed under Open Source Licenses. The C# application, MaxBot, is attempting to implement the same Teriminal User Interface (TUI) experience as the Gemini-CLI but being idomatic to C# and .NET. The comparison I am looking to draw out is how close in behavior, functionality and look and feel is the MaxBot implementations compared to the Gemini CLI.
</task_objective>

<detailed_sequence_steps>
# Compare TUI Implementations Process - Detailed Sequence of Steps

## 1. Understand Gemini-CLI Implementation

1.  Analyze the source code in `tmp/gemini-cli/packages/cli` to understand the TUI implementation.
    - Use `execute_command` with `find tmp/gemini-cli/packages/cli -type f -name "*.tsx" -o -name "*.ts"` to quickly locate UI-related files.
2.  Extensively read, YES READ, the contents of --all-- the files to build a DETAILED mental model of the application's architecture and data flow. Start with `gemini.tsx` and `ui/App.tsx`. Do NOT skim or assume anything, the boss is looking for comprehensive understanding.
3.  Extensively simulate the flow of user input to understand the dynamics of the UI. For example, trace the events that occur when a user types "read the file foo.txt" and presses Enter.
    - NOTE: You must gain understanding of the design dynamics as well as how the terminal gets updated. Be able to answer questions like how is the chatHistory monitored and managed, or how often does the system prompt get sent, or are there user prompts sent to the LLM not from the user but from the tool itself.
4.  Analyze the visual presentation of the UI components. For example, describe the borders, padding, and margins of the `InputPrompt` and `ToolGroupMessage` components.
    - NOTE: I need to better know that you understand how the display is looking and operating as the user and ai assistant are chatting. for example, the user prompt ui has a border display with the text ...., while the api request is occuring the XYZ component is pushed on the terminal and displays an animiation with the text...
5.  Mentally create Mermaid diagrams to illustrate the component relationships and the user input flow.
6.  Mentally capture your findings in a `<gemini-understanding>` tag 
7.  Now create or update any existing documentation found at `docs/references/gemini-cli` as listed below where each document get progressively more detailed.
    - 01_concept_of_operation.md (list essential features to the User. should be terse, pithy and to the point)
    - 02_architecture.md (must be terse, pithy, to the point, and include diagrams like component and sequence diagrams of core application flow(s)).
    - 03_design.md (focus on core features and with diagrams include)
    - IMPORTANT: Only make an update if the existing text is incorrect. When adding addiitonal information, adhere to the formatting conventions already established in the document 
    

## 2. Understand MaxBot Implementation

1.  Analyze the source code in `src/MaxBot.TUI` to understand the MaxBot TUI implementation.
    - Use `execute_command` with `find src/MaxBot.TUI -type f -name "*.cs" -o -name "*.csproj"` to quickly locate UI-related files.
2.  Extensively read, YES READ, the contents of --all-- the files to build a DETAILED mental model of the application's architecture and data flow. Start with `Program.cs`.
3.  Extensively simulate the flow of user input to understand the dynamics of the UI. For example, trace the events that occur when a user types "read the file foo.txt" and presses Enter.
    - NOTE: You must gain understanding of the design dynamics as well as how the terminal gets updated. Be able to answer questions like how is the chatHistory monitored and managed, or how often does the system prompt get sent, or are there user prompts sent to the LLM not from the user but from the tool itself.
4.  Analyze the visual presentation of the UI components. For example, describe the borders, padding, and margins of the `InputPrompt` and `ToolGroupMessage` components.
5.  Mentally create Mermaid diagrams to illustrate the component relationships and the user input flow.
6.  Mentally capture your findings in a `<gemini-understanding>` tag 
7.  Now create or update any existing documentation found at `docs/maxbot-cli` as listed below where each document get progressively more detailed.
    - 01_concept_of_operation.md (list essential features to the User. should be terse, pithy and to the point)
    - 02_architecture.md (must be terse, pithy, to the point, and include diagrams like component and sequence diagrams of core application flow(s)).
    - 03_design.md (focus on core features and with diagrams include)
    - IMPORTANT: Only make an update if the existing text is incorrect. When adding addiitonal information, adhere to the formatting conventions already established in the document 

## 3. Comparing Gemini-CLI and MaxBot

1.  Identify similarities in capability, architecture, and design between gemini-cli and maxbot in a `<similar>` tag and present it to the user for viewing (DO NOT WRITE OUT TO A FILE).
2.  Identify differences in capability, architecture, and design between gemini-cli and maxbot in a `<differences>` tag and present it to the user for viewing (DO NOT WRITE OUT TO A FILE). Minimize focusing on obvious things like C# vs Typescript.
3. Generate a Tool Comparison Case Study in `outputs/tool_comp_case_studies/{{TIMESTAMP}}.md` containing some prose but heavily leverage Tables for comparing this side-by-side.

## 4. What features should MaxBot add next?

1.  The goal of Maxbot is to be an c# open-source project for the .NET community to both learn and leverage and needs to be as ease to you as the amazing gemini-cli. Place the recommendations in a `<recommendation>` tag to help determine what aspects of Gemini CLI should be focused on next by the MaxBot Development team
2.  Finally markdown report for these recommendation in the same directory as the above case study report.
3.  Use the `attempt_completion` tool to only state that reports have been generated and where to find them. The Users explicity asked keep the completion text extremely brief.

</detailed_sequence_steps>

IMPORTANT RULES:
- Only read the files from the folders identified in the workflow. The repo contains a number of outdated documents and even code in some folders that will invalidated the end result.

</task>
