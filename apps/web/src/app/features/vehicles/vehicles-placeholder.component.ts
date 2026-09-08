import { Component } from '@angular/core';

/** Landing page after login until the vehicle list arrives in slice S24. */
@Component({
  selector: 'app-vehicles-placeholder',
  template: `<h1 data-testid="vehicles-heading">Your vehicles</h1><p>Vehicle management arrives in the next slices.</p>`,
})
export class VehiclesPlaceholderComponent {}
