import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toolbar } from 'primeng/toolbar';

/**
 * Application shell: a PrimeNG toolbar with the title and the router outlet.
 * Navigation and authentication state arrive with later slices.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toolbar],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly title = 'Vehicle Maintenance Tracker';
}
