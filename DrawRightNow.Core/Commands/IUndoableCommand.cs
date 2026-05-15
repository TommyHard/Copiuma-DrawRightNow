namespace DrawRightNow.Core.Commands;

/// <summary>
/// Команда в смысле паттерна Command — единица, которая может быть применена
/// (Do) и отменена (Undo). История хранит лёгкие объекты, а не
/// снимки всего холста
/// </summary>
public interface IUndoableCommand
{
    void Do();
    void Undo();
}