import { Pipe, PipeTransform } from '@angular/core';
import { marked } from 'marked';

/**
 * Renders the light markdown the AI returns (bold, lists, headings, inline code).
 * Output is bound via [innerHTML], so Angular's built-in sanitizer still runs over it.
 */
@Pipe({ name: 'md' })
export class MarkdownPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';
    // The model sometimes wraps math in $…$ / $$…$$ — unwrap it to plain text.
    const cleaned = value.replace(/\$\$?([^$\n]+?)\$\$?/g, '$1');
    return marked.parse(cleaned, { async: false, gfm: true, breaks: true }) as string;
  }
}
