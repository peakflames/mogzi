# attempt_completion Tool Specification

## Purpose
Present the result of completed work to the user. This tool marks the end of a task and provides a summary of what was accomplished, optionally with a command to demonstrate the results.

## Parameters
- `result` (required): The final result description - should be comprehensive and final
- `command` (optional): A CLI command to execute to demonstrate the result

## Usage
```xml
<attempt_completion>
<result>
I have successfully created a responsive todo application with the following features:
- Add new tasks with input validation
- Mark tasks as complete/incomplete
- Delete tasks with confirmation
- Filter tasks by status (all, active, completed)
- Persistent storage using localStorage
- Mobile-responsive design with CSS Grid

The application is fully functional and ready for use.
</result>
<command>open index.html</command>
</attempt_completion>
```

## Critical Requirements
- **CANNOT be used until previous tool uses are confirmed successful**
- Must ask yourself: "Have I confirmed from the user that any previous tool uses were successful?"
- If not confirmed, DO NOT use this tool (will result in code corruption)
- Wait for user confirmation of all tool results before attempting completion

## Result Guidelines
- Formulate result in a way that is final and complete
- Do NOT end with questions or offers for further assistance
- Provide comprehensive summary of what was accomplished
- Include key features, functionality, or changes made
- Be specific about deliverables and outcomes
- Avoid conversational language like "Great!" or "Certainly!"

## Command Guidelines
Set `command` for:
- `open index.html` - Display created HTML websites
- `open localhost:3000` - Display locally running development servers
- Commands that demonstrate live results

Do NOT use commands like:
- `echo` or `cat` that merely print text
- Commands that don't show live demonstrations
- Commands that require additional setup

## Best Practices
- Use only after confirming all previous tool operations succeeded
- Provide detailed summary of accomplishments
- Include specific technical details about what was created/modified
- Use demonstration commands when appropriate
- Be direct and technical, not conversational
- Focus on deliverables and functionality

## Common Use Cases
- Completing development tasks (websites, applications, scripts)
- Finishing file modifications and refactoring
- Concluding analysis and documentation tasks
- Presenting results of automation or workflow tasks
- Summarizing complex multi-step operations

## Result Structure
A good result should include:
1. **What was accomplished** - Clear statement of completion
2. **Key features/changes** - Specific details about deliverables
3. **Technical specifications** - Important implementation details
4. **Status** - Confirmation that work is ready/functional

## Examples

### Web Development Completion
```xml
<attempt_completion>
<result>
I have created a fully functional e-commerce product page with the following components:
- Responsive product gallery with image zoom functionality
- Dynamic pricing calculator with quantity discounts
- Add to cart functionality with local storage persistence
- Customer review section with star ratings
- Mobile-optimized layout using CSS Flexbox
- Cross-browser compatibility tested

The page is complete and ready for integration into the larger e-commerce platform.
</result>
<command>open product-page.html</command>
</attempt_completion>
```

### Code Refactoring Completion
```xml
<attempt_completion>
<result>
I have successfully refactored the authentication module with the following improvements:
- Extracted authentication logic into separate AuthService class
- Implemented proper error handling with custom exception types
- Added comprehensive unit tests with 95% code coverage
- Updated all dependent modules to use the new AuthService interface
- Added TypeScript type definitions for better type safety
- Documented all public methods with JSDoc comments

The refactored code is more maintainable, testable, and follows established design patterns.
</result>
</attempt_completion>
```

### Analysis Task Completion
```xml
<attempt_completion>
<result>
I have completed the codebase analysis and generated comprehensive documentation:
- Created detailed architecture overview with component relationships
- Documented all API endpoints with request/response schemas
- Generated code quality report identifying 12 areas for improvement
- Produced dependency analysis showing potential security vulnerabilities
- Created developer onboarding guide with setup instructions
- Established coding standards document based on existing patterns

All documentation is saved in the docs/ directory and ready for team review.
</result>
</attempt_completion>
```

## Error Prevention
- Always wait for user confirmation of tool results
- Never assume tool operations succeeded without explicit confirmation
- Check that all file operations completed successfully
- Verify that commands executed without errors
- Ensure all requirements have been met before completion

## Integration with Workflow
- Final step in task completion process
- Used after all implementation and verification steps
- May be followed by user feedback for improvements
- Marks transition point for new tasks or modifications

## Limitations
- Cannot be used until all previous operations are confirmed
- Should not include questions or requests for further work
- Must provide complete, final summary
- Cannot be used for partial completions or progress updates
