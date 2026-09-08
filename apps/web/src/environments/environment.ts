/**
 * Production build: served by Nginx on the same origin as the proxied API, so the generated
 * client's paths (which already start with /api/v1) need no prefix.
 */
export const environment = {
  apiRootUrl: '',
};
