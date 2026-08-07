# Playwright browser installation timeouts in CI

Date: 2026-08-07

## Conclusion

The failures are caused by the workflow's two-minute step deadline, not by a Playwright-internal browser timeout. Keep Playwright's supported Linux CI command (`install --with-deps`), do not add a browser-binary cache, and give the install enough bounded time:

```yaml
frontend:
  timeout-minutes: 15
  # ...
  - name: Install Playwright Browsers
    run: pnpm --filter='end-to-end-tests' exec playwright install --with-deps chromium
    timeout-minutes: 10

e2e:
  timeout-minutes: 20
  # ...
  - name: Install Playwright Browsers
    run: pnpm --filter='end-to-end-tests' exec playwright install --with-deps chromium firefox
    timeout-minutes: 10
```

Ten minutes is a repository-specific safety bound rather than a Playwright-prescribed value. It is five times the failing limit while still terminating a genuine hang. The larger job limits leave time for build/tests after a slow installation. Playwright's own GitHub Actions example is substantially looser: a 60-minute job and no step-specific install timeout ([Playwright CI documentation](https://playwright.dev/docs/ci#on-pushpull_request)).

## Evidence from this repository

- In [run 31191416751, frontend job 92909012953](https://github.com/FabienDehopre/my-netatmo-24/actions/runs/31191416751/job/92909012953), the install step began at 15:12:57. Playwright entered `Installing dependencies...`; APT determined that nine font packages (21.1 MB) were missing. Fetching `fonts-ipafont-gothic` began at 15:13:05, and the next package did not begin until 15:14:50. GitHub killed the step at 15:15:10. No Playwright browser download had started.
- [Run 30336353176, frontend job 90201977839](https://github.com/FabienDehopre/my-netatmo-24/actions/runs/30336353176/job/90201977839) failed the same way: slow downloads of the same Ubuntu font dependencies were still in progress when the two-minute limit expired.
- These were the two matching failures among the last 100 CI runs examined. Successful installs usually complete well below two minutes, so this is intermittent Ubuntu mirror/network latency exposed by an unusually tight deadline.
- GitHub defines step `timeout-minutes` as the maximum runtime before the process is killed; job `timeout-minutes` independently cancels the whole job ([GitHub workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idsteps-timeout-minutes), [job timeout](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idtimeout-minutes)). Thus the message in both logs is the expected result of the workflow's explicit `timeout-minutes: 2`.

## Why retain `--with-deps`

Playwright documents `playwright install --with-deps chromium` as the combined way to install the browser plus Linux OS dependencies and calls automatic system-dependency installation useful for CI ([browser installation documentation](https://playwright.dev/docs/browsers#install-system-dependencies)). Its general CI recipe is `npx playwright install --with-deps` after package installation ([CI documentation](https://playwright.dev/docs/ci#introduction)). Removing `--with-deps` would make the workflow depend on the changing contents of `ubuntu-latest` and would trade the current timing problem for possible browser-launch failures.

## Why browser caching is not the fix

Playwright explicitly recommends against caching browser binaries: cache restore time is comparable to downloading them, and Linux OS dependencies are not cacheable ([Playwright caching guidance](https://playwright.dev/docs/ci#caching-browsers)). The supplied failure occurred while APT was installing those non-cacheable OS dependencies, before browser binaries were downloaded. The existing `actions/setup-node` pnpm cache also caches package-manager data, not Playwright's system packages ([GitHub dependency-caching reference](https://docs.github.com/en/actions/reference/workflows-and-actions/dependency-caching#setup--actions-for-specific-package-managers)).

## Container alternative

Playwright officially supports running a GitHub Actions job in `mcr.microsoft.com/playwright:<version>-noble`; the image already contains browsers and browser system dependencies, so the runtime install step is omitted ([Playwright container CI example](https://playwright.dev/docs/ci#via-containers), [Docker image contents](https://playwright.dev/docs/docker#introduction)). The image must be pinned to the Playwright version used by the project ([image-tag guidance](https://playwright.dev/docs/docker#image-tags)). GitHub supports this with `jobs.<job_id>.container` ([GitHub container-job documentation](https://docs.github.com/en/actions/how-tos/write-workflows/choose-where-workflows-run/run-jobs-in-a-container)).

This is a valid longer-term option, especially for the frontend browser tests. It is not the minimal fix for the current `e2e` job because that job also installs .NET, Azure tooling, SOPS, and Aspire and starts the distributed app; moving it into the Playwright image needs separate compatibility validation. Raising the bounded timeouts changes only the faulty assumption and preserves the officially supported install path.
