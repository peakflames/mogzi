# Workflow Management Tools Specification

## Overview
Workflow management tools in MaxBot help organize tasks, manage conversations, and handle different operational modes. These tools facilitate task organization, context management, and mode transitions.

## Available Workflow Tools

### new_task

**Purpose**: Create a new task with preloaded context covering the conversation up to this point and key information for continuing with the new task.

**Parameters**:
- `context` (required): Detailed summary of the conversation and key information for the new task

**Usage**:
```xml
<new_task>
<context>
1. Current Work:
   [Detailed description of what was being worked on]

2. Key Technical Concepts:
   - [Concept 1]
   - [Concept 2]

3. Relevant Files and Code:
   - [File Name 1]: [Summary and important snippets]
   - [File Name 2]: [Summary and important snippets]

4. Problem Solving:
   [Detailed description of problems solved]

5. Pending Tasks and Next Steps:
   - [Task 1 details with direct quotes from conversation]
   - [Task 2 details with next steps]
</context>
</new_task>
```

**Context Structure**:
The context should be thorough and include:
1. **Current Work** - What was being worked on prior to the new task request
2. **Key Technical Concepts** - Technologies, frameworks, coding conventions discussed
3. **Relevant Files and Code** - Files examined, modified, or created with code snippets
4. **Problem Solving** - Problems solved and ongoing troubleshooting efforts
5. **Pending Tasks and Next Steps** - Outstanding work with verbatim quotes from conversation

**Best Practices**:
- Be detailed in capturing technical details and architectural decisions
- Include direct quotes from recent conversation for context continuity
- Pay special attention to recent messages and changes
- Enumerate specific files and code sections that are relevant
- Document code patterns and conventions discovered

### plan_mode_respond

**Purpose**: Respond to user inquiries in PLAN MODE for planning solutions and brainstorming.

**Parameters**:
- `response` (required): The response to provide to the user (do not use tools within this parameter)

**Usage**:
```xml
<plan_mode_respond>
<response>
Based on your requirements, I recommend creating a React application with the following architecture:

1. Component Structure:
   - Header component with navigation
   - Main content area with routing
   - Footer component with links

2. State Management:
   - Use React Context for global state
   - Local state for component-specific data

3. Styling Approach:
   - CSS Modules for component styling
   - Responsive design with CSS Grid

Would you like me to proceed with this approach, or would you prefer any modifications to the architecture?
</response>
</plan_mode_respond>
```

**When Available**:
- Only available in PLAN MODE
- Used for conversational planning and brainstorming
- Not available in ACT MODE

**Capabilities**:
- Ask clarifying questions about requirements
- Present detailed plans and architectures
- Brainstorm ideas and solutions
- Engage in back-and-forth planning discussions
- Present visual diagrams (Mermaid) when helpful

## Mode Management

### PLAN MODE vs ACT MODE

**PLAN MODE**:
- Focus on information gathering and planning
- Use `plan_mode_respond` for conversations
- Ask questions to clarify requirements
- Create detailed implementation plans
- Present architectural decisions
- Brainstorm solutions with the user

**ACT MODE**:
- Execute tasks using available tools
- Implement solutions and make changes
- Use all tools except `plan_mode_respond`
- Complete tasks and present results with `attempt_completion`

**Mode Transitions**:
- User must manually toggle between modes
- Cannot programmatically switch modes
- Must direct user to "toggle to Act mode" when needed
- Cannot present toggle options in responses

## Workflow Patterns

### Planning Workflow
1. **Information Gathering** - Use tools to understand current state
2. **Requirement Clarification** - Ask questions using `plan_mode_respond`
3. **Solution Architecture** - Present detailed plans
4. **Plan Refinement** - Iterate on plans based on feedback
5. **Implementation Readiness** - Direct user to switch to ACT MODE

### Task Continuation Workflow
1. **Context Assessment** - Determine if new task is needed
2. **Context Creation** - Gather all relevant information
3. **Task Creation** - Use `new_task` with comprehensive context
4. **Task Transition** - User reviews and approves new task

### Implementation Workflow
1. **Task Execution** - Use appropriate tools iteratively
2. **Progress Verification** - Confirm each step's success
3. **Result Presentation** - Use `attempt_completion` when finished
4. **Feedback Integration** - Handle user feedback for improvements

## Best Practices

### Context Management
- Capture technical details thoroughly
- Include verbatim quotes for continuity
- Document architectural decisions
- Preserve code patterns and conventions
- Note pending work and next steps clearly

### Mode Usage
- Use PLAN MODE for complex planning and architecture
- Use ACT MODE for implementation and execution
- Don't assume mode capabilities - check environment_details
- Guide users on mode transitions when needed

### Task Organization
- Create new tasks for significant context shifts
- Maintain continuity through detailed context
- Preserve important technical information
- Document progress and decisions made

## Integration with Other Tools

### Planning Phase
- Use `read_file` and `list_files` to understand current state
- Use `search_files` to find relevant patterns
- Use `plan_mode_respond` to discuss findings and plans

### Implementation Phase
- Use file tools to implement planned changes
- Use `execute_command` to test and build
- Use `browser_action` to verify results
- Use `attempt_completion` to present final results

### Task Transitions
- Use `new_task` to preserve context across conversations
- Include relevant file contents and code snippets
- Document architectural decisions and patterns
- Preserve user requirements and preferences

## Error Handling
- Mode-specific tools will fail if used in wrong mode
- Context creation requires comprehensive information gathering
- Task transitions need user approval and review
- Plan discussions may require multiple iterations

## Performance Considerations
- Context creation should be thorough but focused
- Planning discussions should be efficient and purposeful
- Task transitions should preserve essential information
- Mode usage should match the current phase of work
