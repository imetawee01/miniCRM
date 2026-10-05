import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthStore } from './auth.store';

describe('authGuard', () => {
  it('redirects anonymous users to login', () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), AuthStore]
    });
    const store = TestBed.inject(AuthStore);
    spyOn(store, 'isAuthenticated').and.returnValue(false);
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/dashboard' } as never)
    );
    expect(String(result)).toContain('login');
  });
});
