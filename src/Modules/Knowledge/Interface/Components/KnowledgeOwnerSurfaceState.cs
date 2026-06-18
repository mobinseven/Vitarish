using MudBlazor;
using Vitarish.Knowledge.Application.Owner;

namespace Vitarish.Knowledge.Interface.Components;

public sealed record KnowledgeOwnerSurfaceSnapshot(
    KnowledgeOwnerReadModel Model,
    KnowledgeOwnerImportFormats ImportFormats,
    bool IsBusy,
    string? ImportSummary,
    string? EvaluationSummary,
    string? Status,
    Color StatusColor,
    int CreateCorpusResetVersion,
    int SourcesVersion)
{
    public static KnowledgeOwnerSurfaceSnapshot Empty(KnowledgeOwnerImportFormats importFormats) => new(
        new KnowledgeOwnerReadModel([], null, []),
        importFormats,
        false,
        null,
        null,
        null,
        Color.Info,
        0,
        0);
}

public sealed class KnowledgeOwnerSurfaceState(IKnowledgeOwnerUseCases ownerUseCases)
{
    public KnowledgeOwnerSurfaceSnapshot Current { get; private set; } = KnowledgeOwnerSurfaceSnapshot.Empty(ownerUseCases.ImportFormats);

    public async Task LoadAsync(string? tenantId, CancellationToken cancellationToken)
    {
        if (!BeginOperation())
        {
            return;
        }

        try
        {
            KnowledgeOwnerReadModel model = await ownerUseCases.LoadAsync(tenantId, null, cancellationToken);
            Current = Current with { Model = model };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ApplyCanceledStatus();
        }
        catch (Exception exception)
        {
            ApplyFailureStatus($"Unable to load knowledge owner state: {exception.Message}");
        }
        finally
        {
            EndOperation();
        }
    }

    public async Task CreateCorpusAsync(string? tenantId, CreateKnowledgeCorpusInput input, CancellationToken cancellationToken)
    {
        await RunCommandAsync(cancellationToken, async token =>
        {
            KnowledgeOwnerCreateCorpusResult result = await ownerUseCases.CreateCorpusAsync(tenantId, input, token);
            ApplyStatus(result.Succeeded, result.Message);

            if (!result.Succeeded || result.Corpus is null)
            {
                return;
            }

            await LoadSelectedCorpusAsync(tenantId, result.Corpus, token);
            Current = Current with
            {
                CreateCorpusResetVersion = Current.CreateCorpusResetVersion + 1,
                ImportSummary = null,
                EvaluationSummary = null,
                SourcesVersion = Current.SourcesVersion + 1
            };
        });
    }

    public async Task SelectCorpusAsync(string? tenantId, KnowledgeOwnerCorpusOption? corpus, CancellationToken cancellationToken)
    {
        await RunCommandAsync(cancellationToken, async token =>
        {
            KnowledgeOwnerReadModel model = await ownerUseCases.SelectCorpusAsync(tenantId, new SelectKnowledgeCorpusInput(corpus), token);
            Current = Current with
            {
                Model = model,
                ImportSummary = null,
                EvaluationSummary = null,
                SourcesVersion = Current.SourcesVersion + 1
            };
        });
    }

    public async Task ImportSourceAsync(string? tenantId, ImportKnowledgeSourceInput input, CancellationToken cancellationToken)
    {
        await RunCommandAsync(cancellationToken, async token =>
        {
            KnowledgeOwnerImportCommandResult result = await ownerUseCases.ImportSourceAsync(tenantId, input, token);
            ApplyStatus(result.Succeeded, result.Message);
            Current = Current with { ImportSummary = result.Summary };

            if (result.Succeeded)
            {
                await ReloadSelectedCorpusAsync(tenantId, token);
            }
        });
    }

    public async Task ReplaceSourceAsync(string? tenantId, ReplaceKnowledgeSourceInput input, CancellationToken cancellationToken)
    {
        await RunCommandAsync(cancellationToken, async token =>
        {
            KnowledgeOwnerImportCommandResult result = await ownerUseCases.ReplaceSourceAsync(tenantId, input, token);
            ApplyStatus(result.Succeeded, result.Message);
            Current = Current with { ImportSummary = result.Summary };

            if (result.Succeeded)
            {
                await ReloadSelectedCorpusAsync(tenantId, token);
            }
        });
    }

    public async Task IndexCorpusAsync(string? tenantId, CancellationToken cancellationToken)
    {
        await RunCommandAsync(cancellationToken, async token =>
        {
            KnowledgeOwnerCommandResult result = await ownerUseCases.IndexCorpusAsync(tenantId, new IndexKnowledgeCorpusInput(Current.Model.SelectedCorpus), token);
            ApplyStatus(result.Succeeded, result.Message);
        });
    }

    public async Task EvaluateCorpusAsync(string? tenantId, CancellationToken cancellationToken)
    {
        await RunCommandAsync(cancellationToken, async token =>
        {
            KnowledgeOwnerEvaluationCommandResult result = await ownerUseCases.EvaluateCorpusAsync(tenantId, new EvaluateKnowledgeCorpusInput(Current.Model.SelectedCorpus), token);
            ApplyStatus(result.Succeeded, result.Message);
            Current = Current with { EvaluationSummary = result.Summary };
        });
    }

    private async Task RunCommandAsync(CancellationToken cancellationToken, Func<CancellationToken, Task> command)
    {
        if (!BeginOperation())
        {
            return;
        }

        try
        {
            await command(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ApplyCanceledStatus();
        }
        catch (Exception exception)
        {
            ApplyFailureStatus($"Knowledge owner operation failed: {exception.Message}");
        }
        finally
        {
            EndOperation();
        }
    }

    private bool BeginOperation()
    {
        if (Current.IsBusy)
        {
            Current = Current with { Status = "Another knowledge owner operation is already running.", StatusColor = Color.Info };
            return false;
        }

        Current = Current with { IsBusy = true, Status = null, StatusColor = Color.Info };
        return true;
    }

    private void EndOperation() => Current = Current with { IsBusy = false };

    private async Task ReloadSelectedCorpusAsync(string? tenantId, CancellationToken cancellationToken)
    {
        await LoadSelectedCorpusAsync(tenantId, Current.Model.SelectedCorpus, cancellationToken);
        Current = Current with { SourcesVersion = Current.SourcesVersion + 1 };
    }

    private async Task LoadSelectedCorpusAsync(string? tenantId, KnowledgeOwnerCorpusOption? selectedCorpus, CancellationToken cancellationToken)
    {
        KnowledgeOwnerReadModel model = await ownerUseCases.LoadAsync(tenantId, selectedCorpus, cancellationToken);
        Current = Current with { Model = model };
    }

    private void ApplyStatus(bool succeeded, string message) => Current = Current with { Status = message, StatusColor = succeeded ? Color.Success : Color.Error };

    private void ApplyFailureStatus(string message) => Current = Current with { Status = message, StatusColor = Color.Error };

    private void ApplyCanceledStatus() => Current = Current with { Status = "The operation was canceled.", StatusColor = Color.Info };
}
