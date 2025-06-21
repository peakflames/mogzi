# ask_followup_question Tool Specification

## Purpose
Ask the user a question to gather additional information needed to complete a task. This tool enables interactive problem-solving and clarification when ambiguities arise or more details are required.

## Parameters
- `question` (required): The question to ask the user - should be clear and specific
- `options` (optional): Array of 2-5 options for the user to choose from

## Usage
```xml
<ask_followup_question>
<question>What type of database would you like to use for this project?</question>
<options>
["PostgreSQL", "MySQL", "SQLite", "MongoDB"]
</options>
</ask_followup_question>
```

## Capabilities
- Enables interactive communication with the user
- Supports multiple choice options for easier responses
- Allows for clarification of ambiguous requirements
- Facilitates gathering of missing information
- Helps refine task specifications

## Best Practices
- Use judiciously to maintain balance between gathering information and avoiding excessive back-and-forth
- Ask clear, specific questions that address the information you need
- Provide options when they can save the user from typing responses manually
- Use when you encounter ambiguities or need additional details to proceed effectively
- Avoid asking questions that can be answered through available tools

## When to Use
- Requirements are ambiguous or unclear
- Multiple valid approaches exist and user preference is needed
- Missing critical information that cannot be inferred
- Need to confirm assumptions before proceeding
- User input is required for configuration or setup choices

## When NOT to Use
- Information can be discovered through other tools (list_files, read_file, etc.)
- Question is not essential for task completion
- Would create unnecessary interruption to workflow
- Answer can be reasonably inferred from context

## Question Types

### Clarification Questions
```xml
<ask_followup_question>
<question>Should the API endpoints return JSON or XML format?</question>
<options>
["JSON", "XML", "Both formats supported"]
</options>
</ask_followup_question>
```

### Preference Questions
```xml
<ask_followup_question>
<question>Which CSS framework would you prefer for styling?</question>
<options>
["Bootstrap", "Tailwind CSS", "Material-UI", "Custom CSS"]
</options>
</ask_followup_question>
```

### Configuration Questions
```xml
<ask_followup_question>
<question>What port should the development server run on?</question>
</ask_followup_question>
```

### Scope Questions
```xml
<ask_followup_question>
<question>Should I include unit tests for the new components?</question>
<options>
["Yes, include unit tests", "No, skip tests for now", "Only basic tests"]
</options>
</ask_followup_question>
```

## Options Guidelines
- Provide 2-5 options when helpful
- Make options mutually exclusive and comprehensive
- Use clear, descriptive option text
- Don't include options to toggle modes (user must do this manually)
- Options should cover the most likely scenarios

## Error Handling
- Returns user's response for processing
- Handles both text responses and option selections
- May receive unexpected responses that need interpretation

## Integration with Other Tools
- Often used before major tool operations when clarification is needed
- May be followed by different tool choices based on user response
- Can influence the entire approach to task completion
- Used to gather requirements before file creation or modification

## Response Processing
- Store user's selection to guide subsequent actions
- Use response to determine which tools to use next
- May need to ask follow-up questions based on initial response
- Adapt workflow based on user preferences

## Examples in Context

### Before File Creation
```xml
<ask_followup_question>
<question>What file format should I use for the configuration?</question>
<options>
["JSON", "YAML", "TOML", "INI"]
</options>
</ask_followup_question>
```

### Before Architecture Decisions
```xml
<ask_followup_question>
<question>Should this be a single-page application or multi-page application?</question>
<options>
["Single-page application (SPA)", "Multi-page application", "Not sure - recommend based on requirements"]
</options>
</ask_followup_question>
```

### Before Tool Selection
```xml
<ask_followup_question>
<question>The file is quite large. Should I use targeted edits or rewrite the entire file?</question>
<options>
["Use targeted edits (safer)", "Rewrite entire file", "Let you decide based on changes needed"]
</options>
</ask_followup_question>
```

## Limitations
- Interrupts workflow and requires user interaction
- Should be used sparingly to maintain efficiency
- Cannot force specific response formats
- User may provide unexpected or unclear responses
